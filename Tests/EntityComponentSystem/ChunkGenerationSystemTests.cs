using System.Diagnostics;
using OpenTK.Mathematics;
using VoxelGame.EntityComponentSystem;
using VoxelGame.EntityComponentSystem.Components;
using VoxelGame.EntityComponentSystem.Systems;
using VoxelGame.GraphicsPipeline;
using VoxelGame.World;
using VoxelGame.World.Data;

namespace Tests.EntityComponentSystem;

/// <summary>
/// Test suite for the ChunkGenerationSystem streaming lifecycle: nearest-first
/// prioritization, cancellation without resurrection, unload with full cleanup,
/// unload hysteresis, and arrival re-meshing of neighbors.
/// </summary>
[Collection("MemorySensitive")]
public class ChunkGenerationSystemTests : IDisposable
{
    /// <summary>
    /// Mock implementation of ITextureProvider for testing purposes.
    /// Provides dummy textures without actual file loading.
    /// </summary>
    private class MockTextureProvider : ITextureProvider
    {
        public Texture GetTexture(string name) => new MockTexture();
    }

    /// <summary>
    /// Mock implementation of Texture for testing purposes.
    /// Provides no-op implementations of required methods.
    /// </summary>
    private class MockTexture : Texture
    {
        public MockTexture()
            : base("") { } // Pass empty string to base to prevent file loading

        public override void Bind() { }

        public override void Dispose() { }
    }

    private readonly EntityManager _entityManager;
    private readonly WorldSystem _worldSystem;
    private readonly ChunkGenerationSystem _chunkGenerationSystem;

    public ChunkGenerationSystemTests()
    {
        _entityManager = new EntityManager();
        _worldSystem = new WorldSystem();
        _chunkGenerationSystem = new ChunkGenerationSystem(
            _entityManager,
            _worldSystem,
            new MockTextureProvider(),
            workerCount: 1
        );
    }

    public void Dispose()
    {
        _chunkGenerationSystem.Dispose();
        _entityManager.Dispose();
        GC.SuppressFinalize(this);
    }

    private bool WaitFor(Func<bool> condition, int timeoutMs = 30000)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            _chunkGenerationSystem.Update();
            if (condition())
            {
                return true;
            }
            Thread.Sleep(5);
        }
        _chunkGenerationSystem.Update();
        return condition();
    }

    private void PumpFrames(int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            _chunkGenerationSystem.Update();
            Thread.Sleep(5);
        }
    }

    /// <summary>
    /// Verifies the pending queue dequeues nearest chunks first, keyed on
    /// squared distance to the active center.
    /// </summary>
    [Fact]
    public void PendingChunks_AreOrderedNearestFirst()
    {
        _chunkGenerationSystem.UpdateChunks(new ChunkPosition(0, 0), 2);

        IReadOnlyList<ChunkPosition> pending = _chunkGenerationSystem.GetPendingOrder();

        Assert.NotEmpty(pending);
        long previousDistance = -1;
        foreach (ChunkPosition position in pending)
        {
            long distance =
                (long)position.X * position.X + (long)position.Z * position.Z;
            Assert.True(
                distance >= previousDistance,
                $"Chunk {position} at distance {distance} dequeues after distance {previousDistance}"
            );
            previousDistance = distance;
        }
    }

    /// <summary>
    /// Regression test: chunks cancelled before their upload must never be
    /// resurrected by late-finishing generation work.
    /// </summary>
    [Fact]
    public void CancelledChunks_AreNeverResurrected()
    {
        ChunkPosition origin = new(0, 0);
        ChunkPosition farCenter = new(30, 0);

        _chunkGenerationSystem.UpdateChunks(origin, 1);
        // Move away before all queued chunks were uploaded; queued and
        // in-flight work gets cancelled.
        _chunkGenerationSystem.UpdateChunks(farCenter, 1);

        Assert.True(WaitFor(() => _chunkGenerationSystem.GetChunkState(farCenter) == ChunkState.Active));
        // Drain any stray ready items that completed before cancellation landed.
        PumpFrames(30);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                ChunkPosition position = new(origin.X + dx, origin.Z + dz);
                Assert.False(_worldSystem.ChunkExists(position));
                Assert.Null(_chunkGenerationSystem.GetChunkState(position));
            }
        }
    }

    /// <summary>
    /// Verifies that unloading a chunk removes its entity, its world data, and
    /// disposes its MeshComponent (leak-free GPU cleanup). Also verifies the
    /// disposed component (and its mesh arrays) become collectable — historical
    /// component pooling retained them forever, growing memory while walking.
    /// </summary>
    [Fact]
    public void Unload_RemovesEntityAndWorldDataAndDisposesComponent()
    {
        ChunkPosition origin = new(0, 0);
        _chunkGenerationSystem.UpdateChunks(origin, 1);

        Assert.True(WaitFor(() => _chunkGenerationSystem.GetChunkState(origin) == ChunkState.Active));

        (int entity, WeakReference meshRef) = TrackActiveChunkMesh(origin);

        _chunkGenerationSystem.UpdateChunks(new ChunkPosition(30, 0), 1);

        Assert.False(_worldSystem.ChunkExists(origin));
        Assert.Null(_chunkGenerationSystem.GetChunkState(origin));
        Assert.False(_entityManager.EntityExists(entity));
        Assert.Null(_entityManager.GetComponent<MeshComponent>(entity));
        AssertMeshDisposed(origin, meshRef);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(meshRef.IsAlive);
    }

    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.NoInlining
    )]
    private (int Entity, WeakReference MeshRef) TrackActiveChunkMesh(ChunkPosition position)
    {
        int entity = _chunkGenerationSystem.GetChunkEntity(position);
        Assert.NotEqual(-1, entity);
        Assert.True(_entityManager.EntityExists(entity));
        MeshComponent? meshComponent = _entityManager.GetComponent<MeshComponent>(entity);
        Assert.NotNull(meshComponent);
        return (entity, new WeakReference(meshComponent));
    }

    /// <summary>
    /// Verifies the unloaded chunk's MeshComponent was disposed before release.
    /// Runs while the component is normally still unreachable-but-uncollected;
    /// if the GC beat us to it the object was already released, which is the
    /// property under test anyway.
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.NoInlining
    )]
    private static void AssertMeshDisposed(ChunkPosition position, WeakReference meshRef)
    {
        if (meshRef.Target is MeshComponent meshComponent)
        {
            Assert.True(meshComponent.IsDisposed, $"MeshComponent for {position} was not disposed on unload");
        }
    }

    /// <summary>
    /// Verifies unload hysteresis: a chunk at radius+1 stays active, while a
    /// chunk beyond radius+hysteresis unloads.
    /// </summary>
    [Fact]
    public void Hysteresis_KeepsChunksJustOutsideRadius_AndUnloadsBeyondHysteresis()
    {
        ChunkPosition edgeChunk = new(3, 0);
        _chunkGenerationSystem.UpdateChunks(new ChunkPosition(0, 0), 3);

        Assert.True(WaitFor(() => _chunkGenerationSystem.GetChunkState(edgeChunk) == ChunkState.Active));

        // Move the center one chunk left: edgeChunk is now at distance 4,
        // which is > radius (3) but <= radius + hysteresis (5) → stays loaded.
        _chunkGenerationSystem.UpdateChunks(new ChunkPosition(-1, 0), 3);
        PumpFrames(10);
        Assert.Equal(ChunkState.Active, _chunkGenerationSystem.GetChunkState(edgeChunk));
        Assert.True(_worldSystem.ChunkExists(edgeChunk));

        // Move further: distance 7 > radius + hysteresis (5) → unloads.
        _chunkGenerationSystem.UpdateChunks(new ChunkPosition(-4, 0), 3);
        PumpFrames(10);
        Assert.Null(_chunkGenerationSystem.GetChunkState(edgeChunk));
        Assert.False(_worldSystem.ChunkExists(edgeChunk));
    }

    /// <summary>
    /// Verifies that generating a new chunk re-meshes its active neighbors so
    /// previously exposed border faces get culled (no world-edge holes).
    /// </summary>
    [Fact]
    public void Arrival_RemeshesActiveNeighbors()
    {
        ChunkPosition center = new(0, 0);
        _chunkGenerationSystem.UpdateChunks(center, 0);

        Assert.True(WaitFor(() => _chunkGenerationSystem.GetChunkState(center) == ChunkState.Active));
        PumpFrames(2);

        int entity = _chunkGenerationSystem.GetChunkEntity(center);
        MeshComponent meshComponent = _entityManager.GetComponent<MeshComponent>(entity)!;
        ChunkMeshData originalMesh = meshComponent.MeshData;
        int originalVertexCount = originalMesh.Vertices.Length;

        // Grow the radius so 4 neighbors arrive around the lonely chunk.
        _chunkGenerationSystem.UpdateChunks(center, 1);

        Assert.True(WaitFor(() =>
            _chunkGenerationSystem.GetChunkState(new ChunkPosition(1, 0)) == ChunkState.Active
            && _chunkGenerationSystem.GetChunkState(new ChunkPosition(-1, 0)) == ChunkState.Active
            && _chunkGenerationSystem.GetChunkState(new ChunkPosition(0, 1)) == ChunkState.Active
            && _chunkGenerationSystem.GetChunkState(new ChunkPosition(0, -1)) == ChunkState.Active
        ));
        PumpFrames(2);

        Assert.NotSame(originalMesh, meshComponent.MeshData);
        // Faces toward the arrived neighbors are now culled, so the re-meshed
        // chunk must have fewer vertices.
        Assert.True(meshComponent.MeshData.Vertices.Length < originalVertexCount);
    }

    /// <summary>
    /// Verifies that chunk streaming adds chunks around the player and removes
    /// chunks left behind when the player moves.
    /// </summary>
    [Fact]
    public void UpdateChunks_AddsAndRemovesChunksBasedOnCenter()
    {
        ChunkPosition origin = new(0, 0);
        _chunkGenerationSystem.UpdateChunks(origin, 1);

        Assert.True(WaitFor(() => _chunkGenerationSystem.ActiveChunkCount == 5));
        Assert.True(_worldSystem.ChunkExists(origin));

        ChunkPosition newCenter = new(4, 4);
        _chunkGenerationSystem.UpdateChunks(newCenter, 1);

        Assert.True(WaitFor(() =>
            _chunkGenerationSystem.ActiveChunkCount == 5
            && _chunkGenerationSystem.GetPendingOrder().Count == 0
        ));

        Assert.True(_worldSystem.ChunkExists(newCenter));
        Assert.False(_worldSystem.ChunkExists(origin));
    }

    /// <summary>
    /// Verifies that activating a chunk creates an entity with Mesh, Transform
    /// (at the chunk's world origin), and Texture components.
    /// </summary>
    [Fact]
    public void ChunkActivation_CreatesCorrectEntityComponents()
    {
        ChunkPosition center = new(2, 3);
        _chunkGenerationSystem.UpdateChunks(center, 0);

        Assert.True(WaitFor(() => _chunkGenerationSystem.GetChunkState(center) == ChunkState.Active));
        PumpFrames(2);

        List<int> meshEntities = [.. _entityManager.GetEntitiesWithComponent<MeshComponent>()];
        List<int> transformEntities = [.. _entityManager.GetEntitiesWithComponent<TransformComponent>()];
        List<int> textureEntities = [.. _entityManager.GetEntitiesWithComponent<TextureComponent>()];

        Assert.Single(meshEntities);
        Assert.Single(transformEntities);
        Assert.Single(textureEntities);

        TransformComponent? transform = _entityManager.GetComponent<TransformComponent>(transformEntities[0]);
        Assert.NotNull(transform);
        Assert.Equal(new Vector3(center.WorldX, 0, center.WorldZ), transform.Position);

        MeshComponent? meshComponent = _entityManager.GetComponent<MeshComponent>(meshEntities[0]);
        Assert.NotNull(meshComponent);
        Assert.False(meshComponent.MeshData.IsEmpty);
        Assert.True(meshComponent.MeshData.Uses16BitIndices);
    }

    /// <summary>
    /// Ensures that updating chunks with the same center and radius is a no-op
    /// that does not enqueue duplicate work.
    /// </summary>
    [Fact]
    public void UpdateChunks_WithSameCenterAndRadius_DoesNothing()
    {
        ChunkPosition center = new(0, 0);
        _chunkGenerationSystem.UpdateChunks(center, 1);

        Assert.True(WaitFor(() => _chunkGenerationSystem.ActiveChunkCount == 5));

        int entityCountBefore = _entityManager.EntityCount;

        _chunkGenerationSystem.UpdateChunks(center, 1);
        PumpFrames(10);

        Assert.Empty(_chunkGenerationSystem.GetPendingOrder());
        Assert.Equal(5, _chunkGenerationSystem.ActiveChunkCount);
        Assert.Equal(entityCountBefore, _entityManager.EntityCount);
    }

    /// <summary>
    /// Verifies that a chunk whose generation fails transiently is re-enqueued
    /// and eventually activates instead of leaving a permanent hole (the old
    /// behavior dropped the handle on the first failure and never retried it
    /// until the player moved).
    /// </summary>
    [Fact]
    public void TransientGenerationFailure_IsRetriedUntilSuccess()
    {
        ChunkPosition target = new(0, 0);
        int attempts = 0;
        _chunkGenerationSystem.GenerationOverride = (position, worldSystem) =>
        {
            if (position == target && Interlocked.Increment(ref attempts) <= 2)
            {
                throw new InvalidOperationException("simulated transient failure");
            }
            ChunkData data = ChunkGenerator.GenerateChunkData(position);
            return (ChunkGenerator.GenerateChunkMesh(position, worldSystem, data), data);
        };

        _chunkGenerationSystem.UpdateChunks(target, 0);

        Assert.True(WaitFor(() => _chunkGenerationSystem.GetChunkState(target) == ChunkState.Active));
        Assert.Equal(3, attempts);
        Assert.True(_worldSystem.ChunkExists(target));
    }

    /// <summary>
    /// Verifies that a chunk whose generation always fails is given up on after
    /// the bounded number of attempts — the handle is dropped instead of the
    /// failure spinning the retry loop forever.
    /// </summary>
    [Fact]
    public void PersistentGenerationFailure_GivesUpAfterBoundedRetries()
    {
        ChunkPosition target = new(5, 0);
        int attempts = 0;
        _chunkGenerationSystem.GenerationOverride = (position, worldSystem) =>
        {
            if (position == target)
            {
                Interlocked.Increment(ref attempts);
                throw new InvalidOperationException("simulated persistent failure");
            }
            ChunkData data = ChunkGenerator.GenerateChunkData(position);
            return (ChunkGenerator.GenerateChunkMesh(position, worldSystem, data), data);
        };

        _chunkGenerationSystem.UpdateChunks(target, 0);

        Assert.True(WaitFor(() => _chunkGenerationSystem.GetChunkState(target) == null));
        Assert.Equal(3, attempts);
        Assert.False(_worldSystem.ChunkExists(target));
        Assert.Equal(-1, _chunkGenerationSystem.GetChunkEntity(target));
    }

    /// <summary>
    /// Verifies that disposing the streamer is a complete teardown: all active
    /// chunks are unloaded (entities removed, mesh buffers disposed, world data
    /// dropped), nothing outlives the system, and disposal is idempotent.
    /// </summary>
    [Fact]
    public void Dispose_FullyUnloadsAllChunksAndIsIdempotent()
    {
        ChunkPosition origin = new(0, 0);
        _chunkGenerationSystem.UpdateChunks(origin, 1);

        Assert.True(WaitFor(() => _chunkGenerationSystem.ActiveChunkCount == 5));

        int originEntity = _chunkGenerationSystem.GetChunkEntity(origin);
        MeshComponent? meshComponent = _entityManager.GetComponent<MeshComponent>(originEntity);
        Assert.NotNull(meshComponent);

        _chunkGenerationSystem.Dispose();

        Assert.Equal(0, _chunkGenerationSystem.ActiveChunkCount);
        Assert.Empty(_worldSystem.GetAllChunkPositions());
        Assert.Equal(0, _entityManager.EntityCount);
        Assert.False(_entityManager.EntityExists(originEntity));
        Assert.True(meshComponent.IsDisposed);
        Assert.Null(_chunkGenerationSystem.GetChunkState(new ChunkPosition(1, 0)));

        // Disposal must be safely repeatable (Game.OnUnload may run again).
        _chunkGenerationSystem.Dispose();
    }
}
