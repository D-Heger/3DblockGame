using System.Collections.Concurrent;
using OpenTK.Mathematics;
using VoxelGame.EntityComponentSystem.Components;
using VoxelGame.GraphicsPipeline;
using VoxelGame.World;
using VoxelGame.World.Data;

namespace VoxelGame.EntityComponentSystem.Systems;

public interface ITextureProvider
{
    public Texture GetTexture(string name);
}

public class DefaultTextureProvider : ITextureProvider
{
    public Texture GetTexture(string name) => new(name);
}

public enum ChunkState
{
    Queued,
    Generating,
    Ready,
    Active,
}

public sealed class ChunkHandle
{
    public ChunkState State;
    public readonly CancellationTokenSource Cancellation = new();
    public ChunkData? Data;
    public ChunkMeshData? Mesh;
    public int Entity = -1;

    // Number of generation attempts so far; bounded by MaxGenerationAttempts.
    public int Attempts;
}

/// <summary>
/// Streams chunk columns around a moving center: nearest-first prioritized
/// generation on dedicated worker threads, cancellation of obsolete work,
/// hysteresis unloading, and leak-free GPU cleanup. All state transitions and
/// GL-facing operations happen on the main thread (Update/UpdateChunks);
/// workers only produce ChunkData + ChunkMeshData for claimed handles.
/// </summary>
public class ChunkGenerationSystem : System, IDisposable
{
    // Maximum number of chunk activations/re-meshes to process per frame
    private const int MaxChunksPerFrame = 8;

    // Chunks stay loaded until they fall more than this many chunks outside the radius
    private const int UnloadHysteresis = 2;

    // Failed chunk generations are re-enqueued until this bound. Generation is
    // deterministic, so a persistent fault indicates a bug rather than transient
    // contention; the cap keeps such faults from spinning the retry loop.
    private const int MaxGenerationAttempts = 3;

    private readonly EntityManager _entityManager;
    private readonly WorldSystem _worldSystem;
    private readonly ITextureProvider _textureProvider;

    // Chunk lifecycle state and the nearest-first pending heap (min-heap keyed
    // on squared distance to the active center). Guarded by a SINGLE lock so
    // there is no lock ordering to invert; workers also Monitor.Wait/Pulse on
    // it and atomically pop + claim their work item under it.
    private readonly Dictionary<ChunkPosition, ChunkHandle> _handles = [];
    private readonly List<(long Key, ChunkPosition Position)> _pending = [];
    private readonly object _lock = new();

    // Center the pending heap is currently keyed on (set in
    // RebuildPendingHeap). Retry pushes between rebuilds use it for their
    // distance keys. Guarded by _lock.
    private ChunkPosition _heapCenter;
    private bool _disposed;

    // Chunks that finished generating, awaiting main-thread activation.
    private readonly ConcurrentQueue<ChunkPosition> _ready = [];

    // Active neighbors that need their mesh regenerated (arrival/unload fixup).
    // Main-thread owned.
    private readonly Queue<ChunkPosition> _remeshRequests = [];

    // Dedicated generation worker threads.
    private readonly Thread[] _workers;
    private bool _shutdown;

    // Shared texture for all chunks - lazy initialized on the main thread
    private Texture? _sharedTexture;

    // Last center and radius processed by UpdateChunks
    private ChunkPosition? _lastCenter;
    private int? _lastRadius;

    // Test/benchmark seam: overrides chunk generation when set.
    internal Func<ChunkPosition, WorldSystem, (ChunkMeshData Mesh, ChunkData Data)>? GenerationOverride;

    public ChunkGenerationSystem(
        EntityManager entityManager,
        WorldSystem worldSystem,
        ITextureProvider? textureProvider = null,
        int workerCount = 0
    )
    {
        _entityManager = entityManager;
        _worldSystem = worldSystem;
        _textureProvider = textureProvider ?? new DefaultTextureProvider();

        int count = workerCount > 0 ? workerCount : Math.Max(2, Environment.ProcessorCount / 2);
        _workers = new Thread[count];
        for (int i = 0; i < count; i++)
        {
            _workers[i] = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = $"ChunkWorker-{i}",
            };
            _workers[i].Start();
        }
    }

    /// <summary>
    /// Updates the active chunk area around the given chunk-space center.
    /// Should be called once per frame on the main thread.
    /// </summary>
    public void UpdateChunks(ChunkPosition center, int radius)
    {
        if (_lastCenter == center && _lastRadius == radius)
        {
            return;
        }

        _lastCenter = center;
        _lastRadius = radius;

        long unloadDistanceSquared = (long)(radius + UnloadHysteresis) * (radius + UnloadHysteresis);
        long radiusSquared = (long)radius * radius;

        lock (_lock)
        {
            // Unload chunks that fell outside radius + hysteresis
            List<ChunkPosition> toUnload = [];
            foreach (KeyValuePair<ChunkPosition, ChunkHandle> entry in _handles)
            {
                if (DistanceSquared(entry.Key, center) > unloadDistanceSquared)
                {
                    toUnload.Add(entry.Key);
                }
            }
            foreach (ChunkPosition position in toUnload)
            {
                UnloadChunk(position);
            }

            // Enqueue missing chunks within the circular target area, nearest first
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if ((long)dx * dx + (long)dz * dz > radiusSquared)
                    {
                        continue;
                    }

                    ChunkPosition position = new(center.X + dx, center.Z + dz);
                    if (!_handles.ContainsKey(position))
                    {
                        _handles[position] = new ChunkHandle { State = ChunkState.Queued };
                    }
                }
            }

            RebuildPendingHeap(center);
        }
    }

    /// <summary>
    /// Activates ready chunks and processes neighbor re-mesh requests, both
    /// bounded by a shared per-frame budget. Should be called once per frame
    /// on the main thread.
    /// </summary>
    public void Update()
    {
        int processed = 0;
        while (processed < MaxChunksPerFrame && _ready.TryDequeue(out ChunkPosition position))
        {
            lock (_lock)
            {
                if (
                    !_handles.TryGetValue(position, out ChunkHandle? handle)
                    || handle.State != ChunkState.Ready
                    || handle.Cancellation.IsCancellationRequested
                    || handle.Mesh == null
                    || handle.Data == null
                )
                {
                    // Cancelled or unloaded before upload: never resurrect.
                    continue;
                }

                ActivateChunk(position, handle);
            }
            processed++;
        }

        // Neighbor re-meshes share the same budget so unload/arrival rings
        // cannot stall the frame; left-over requests carry into later frames.
        while (processed < MaxChunksPerFrame && _remeshRequests.Count > 0)
        {
            RegenerateChunkMesh(_remeshRequests.Dequeue());
            processed++;
        }
    }

    /// <summary>
    /// Signals workers to stop, joins them, then fully unloads every chunk
    /// (entities, components, GL buffers, and world data) and releases the
    /// shared texture. Idempotent; safe to call from <c>Game.OnUnload</c> and
    /// again from test disposal.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        lock (_lock)
        {
            _shutdown = true;
            // Drop unclaimed work so workers exit immediately instead of
            // draining the queue (Join would otherwise wait for every pending
            // chunk of a large view distance).
            _pending.Clear();
            Monitor.PulseAll(_lock);
        }

        foreach (Thread worker in _workers)
        {
            worker.Join();
        }

        lock (_lock)
        {
            // Snapshot first: UnloadChunk removes handles from the dictionary.
            List<ChunkPosition> toUnload = [.. _handles.Keys];
            foreach (ChunkPosition position in toUnload)
            {
                UnloadChunk(position);
            }

            while (_ready.TryDequeue(out _)) { }
            _remeshRequests.Clear();
        }

        _sharedTexture?.Dispose();
        _sharedTexture = null;
        GC.SuppressFinalize(this);
    }

    private void ActivateChunk(ChunkPosition position, ChunkHandle handle)
    {
        int entity = _entityManager.CreateEntity();

        _entityManager.AddComponent(
            entity,
            new TransformComponent(
                new Vector3(position.WorldX, 0, position.WorldZ),
                Quaternion.Identity,
                Vector3.One
            )
        );
        _entityManager.AddComponent(entity, new MeshComponent(handle.Mesh!));
        _entityManager.AddComponent(entity, new TextureComponent(GetSharedTexture()));

        handle.Entity = entity;
        _worldSystem.AddChunk(position, handle.Data!);

        // WorldSystem/ChunkComponent own the data from here on.
        handle.Mesh = null;
        handle.Data = null;
        handle.State = ChunkState.Active;

        // After the chunk is fully generated, update neighboring chunks
        ScheduleNeighborRemesh(position);
    }

    private void UnloadChunk(ChunkPosition position)
    {
        if (!_handles.Remove(position, out ChunkHandle? handle))
        {
            return;
        }

        handle.Cancellation.Cancel();
        // The handle has left _handles, so no future lookup can race on this
        // token; the only remaining readers are in-flight workers, which only
        // observe the sticky cancellation state (safe after Dispose).
        handle.Cancellation.Dispose();

        if (handle.State == ChunkState.Active)
        {
            if (handle.Entity >= 0 && _entityManager.EntityExists(handle.Entity))
            {
                int entity = handle.Entity;
                _entityManager.GetComponent<MeshComponent>(entity)?.Dispose();
                _entityManager.RemoveComponent<TransformComponent>(entity);
                _entityManager.RemoveComponent<MeshComponent>(entity);
                _entityManager.RemoveComponent<TextureComponent>(entity);
                _entityManager.RemoveEntity(entity);
            }

            _worldSystem.RemoveChunk(position);

            // Neighbors keep their culled border faces unless re-meshed on unload.
            ScheduleNeighborRemesh(position);
        }
    }

    private void ScheduleNeighborRemesh(ChunkPosition position)
    {
        ReadOnlySpan<ChunkPosition> neighborOffsets =
        [
            new(position.X + 1, position.Z),
            new(position.X - 1, position.Z),
            new(position.X, position.Z + 1),
            new(position.X, position.Z - 1),
        ];

        foreach (ChunkPosition neighborPosition in neighborOffsets)
        {
            if (
                _handles.TryGetValue(neighborPosition, out ChunkHandle? neighbor)
                && neighbor.State == ChunkState.Active
                && !_remeshRequests.Contains(neighborPosition)
            )
            {
                _remeshRequests.Enqueue(neighborPosition);
            }
        }
    }

    private void RegenerateChunkMesh(ChunkPosition position)
    {
        int entity;
        lock (_lock)
        {
            if (
                !_handles.TryGetValue(position, out ChunkHandle? handle)
                || handle.State != ChunkState.Active
                || handle.Entity < 0
                || !_entityManager.EntityExists(handle.Entity)
            )
            {
                return;
            }
            entity = handle.Entity;
        }

        // Re-mesh from the registered block data; regenerating ChunkData here
        // would duplicate (then discard) the expensive block-generation pass.
        ChunkData? chunkData = _worldSystem.GetChunk(position);
        if (chunkData == null)
        {
            return;
        }

        ChunkMeshData newMeshData = ChunkGenerator.GenerateChunkMesh(position, _worldSystem, chunkData);
        if (newMeshData.IsEmpty)
        {
            return;
        }

        MeshComponent? meshComponent = _entityManager.GetComponent<MeshComponent>(entity);
        if (meshComponent == null || meshComponent.IsDisposed)
        {
            return;
        }

        meshComponent.MeshData = newMeshData;

        // Force buffer recreation on next render
        meshComponent.ResetBuffers();
    }

    private Texture GetSharedTexture() => _sharedTexture ??= _textureProvider.GetTexture("atlas");

    private void WorkerLoop()
    {
        while (true)
        {
            ChunkPosition position = default;
            ChunkHandle? handle = null;

            lock (_lock)
            {
                while (_pending.Count == 0 && !_shutdown)
                {
                    Monitor.Wait(_lock);
                }

                if (_shutdown)
                {
                    return;
                }

                // Pop until a claimable chunk is found; stale entries
                // (unloaded, cancelled, or already claimed) are discarded.
                while (_pending.Count > 0)
                {
                    ChunkPosition candidate = HeapPop().Position;
                    if (
                        _handles.TryGetValue(candidate, out ChunkHandle? claimed)
                        && claimed.State == ChunkState.Queued
                        && !claimed.Cancellation.IsCancellationRequested
                    )
                    {
                        claimed.State = ChunkState.Generating;
                        claimed.Attempts++;
                        position = candidate;
                        handle = claimed;
                        break;
                    }
                }

                if (handle == null)
                {
                    // Heap held only stale entries; wait again.
                    continue;
                }
            }

            (ChunkMeshData Mesh, ChunkData Data) generated;
            try
            {
                generated = GenerationOverride != null
                    ? GenerationOverride(position, _worldSystem)
                    : GenerateChunk(position);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"Chunk generation failed for {position} (attempt {handle.Attempts}): {e.Message}"
                );
                lock (_lock)
                {
                    if (
                        _handles.TryGetValue(position, out ChunkHandle? failed)
                        && ReferenceEquals(failed, handle)
                        && failed.State == ChunkState.Generating
                        && !failed.Cancellation.IsCancellationRequested
                    )
                    {
                        if (failed.Attempts < MaxGenerationAttempts)
                        {
                            // Re-enqueue for retry so a transient fault does not
                            // leave a permanent hole. Attempts is only incremented
                            // on claim under _lock, so the retry count is bounded
                            // even with multiple workers.
                            failed.State = ChunkState.Queued;
                            HeapPush((DistanceSquared(position, _heapCenter), position));
                            Monitor.PulseAll(_lock);
                        }
                        else
                        {
                            // Bounded retries exhausted: give up on this chunk.
                            _handles.Remove(position);
                        }
                    }
                }
                continue;
            }

            lock (_lock)
            {
                if (
                    handle.State == ChunkState.Generating
                    && !handle.Cancellation.IsCancellationRequested
                    && _handles.TryGetValue(position, out ChunkHandle? current)
                    && ReferenceEquals(current, handle)
                )
                {
                    handle.Mesh = generated.Mesh;
                    handle.Data = generated.Data;
                    handle.State = ChunkState.Ready;
                    _ready.Enqueue(position);
                }
                // Otherwise the chunk was cancelled/unloaded mid-generation:
                // discard the results instead of resurrecting it.
            }
        }
    }

    private (ChunkMeshData Mesh, ChunkData Data) GenerateChunk(ChunkPosition position)
    {
        ChunkMeshData mesh = ChunkGenerator.GenerateChunkMesh(position, _worldSystem, out ChunkData data);
        return (mesh, data);
    }

    private static long DistanceSquared(ChunkPosition position, ChunkPosition center)
    {
        long dx = position.X - center.X;
        long dz = position.Z - center.Z;
        return dx * dx + dz * dz;
    }

    // Caller must hold _lock.
    private void HeapPush((long Key, ChunkPosition Position) item)
    {
        _pending.Add(item);
        int index = _pending.Count - 1;
        while (index > 0)
        {
            int parent = (index - 1) >> 1;
            if (_pending[parent].Key <= _pending[index].Key)
            {
                break;
            }
            (_pending[parent], _pending[index]) = (_pending[index], _pending[parent]);
            index = parent;
        }
    }

    // Caller must hold _lock.
    private (long Key, ChunkPosition Position) HeapPop()
    {
        (long Key, ChunkPosition Position) top = _pending[0];
        int last = _pending.Count - 1;
        _pending[0] = _pending[last];
        _pending.RemoveAt(last);

        int index = 0;
        while (true)
        {
            int left = 2 * index + 1;
            int right = left + 1;
            int smallest = index;

            if (left < _pending.Count && _pending[left].Key < _pending[smallest].Key)
            {
                smallest = left;
            }
            if (right < _pending.Count && _pending[right].Key < _pending[smallest].Key)
            {
                smallest = right;
            }
            if (smallest == index)
            {
                break;
            }

            (_pending[smallest], _pending[index]) = (_pending[index], _pending[smallest]);
            index = smallest;
        }

        return top;
    }

    // Rebuild the pending heap keyed on the new center so the nearest chunks
    // generate first. Caller must hold _lock.
    private void RebuildPendingHeap(ChunkPosition center)
    {
        _heapCenter = center;
        _pending.Clear();
        foreach (KeyValuePair<ChunkPosition, ChunkHandle> entry in _handles)
        {
            if (entry.Value.State == ChunkState.Queued)
            {
                HeapPush((DistanceSquared(entry.Key, center), entry.Key));
            }
        }
        Monitor.PulseAll(_lock);
    }

    internal ChunkState? GetChunkState(ChunkPosition position)
    {
        lock (_lock)
        {
            return _handles.TryGetValue(position, out ChunkHandle? handle) ? handle.State : null;
        }
    }

    internal int GetChunkEntity(ChunkPosition position)
    {
        lock (_lock)
        {
            return _handles.TryGetValue(position, out ChunkHandle? handle) ? handle.Entity : -1;
        }
    }

    internal int ActiveChunkCount
    {
        get
        {
            lock (_lock)
            {
                int count = 0;
                foreach (ChunkHandle handle in _handles.Values)
                {
                    if (handle.State == ChunkState.Active)
                    {
                        count++;
                    }
                }
                return count;
            }
        }
    }

    /// <summary>
    /// Snapshot of pending chunk positions in nearest-first dequeue order.
    /// </summary>
    internal IReadOnlyList<ChunkPosition> GetPendingOrder()
    {
        lock (_lock)
        {
            List<(long Key, ChunkPosition Position)> snapshot = [.. _pending];
            snapshot.Sort(static (a, b) => a.Key.CompareTo(b.Key));
            return snapshot.ConvertAll(static entry => entry.Position);
        }
    }
}
