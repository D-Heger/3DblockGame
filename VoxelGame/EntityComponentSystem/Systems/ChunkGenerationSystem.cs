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

public class ChunkGenerationSystem(
    EntityManager entityManager,
    WorldSystem worldSystem,
    ITextureProvider? textureProvider = null
) : System
{
    private readonly EntityManager _entityManager = entityManager;
    private readonly WorldSystem _worldSystem = worldSystem;
    private readonly ITextureProvider _textureProvider =
        textureProvider ?? new DefaultTextureProvider();

    // Stores active chunk entities with their positions
    private readonly ConcurrentDictionary<ChunkPosition, int> _chunkEntities = new();

    // Queue of chunks to generate
    private readonly ConcurrentQueue<ChunkPosition> _chunksToGenerate = new();

    // Set of currently active chunk positions
    private readonly ConcurrentDictionary<ChunkPosition, bool> _activeChunkPositions = new();

    // Queue of chunks awaiting processing on the main thread
    private readonly ConcurrentQueue<(
        ChunkPosition chunkPosition,
        ChunkMeshData chunkMeshData,
        ChunkData chunkData
    )> _chunksAwaitingMainThreadProcessing = new();

    // Shared texture for all chunks - lazy initialized
    private Texture? _sharedTexture;
    private readonly Lock _textureLock = new();

    // Last player chunk center and render distance processed by UpdateChunks
    private ChunkPosition? _lastCenter;
    private int? _lastRenderDistance;

    private Texture GetSharedTexture()
    {
        if (_sharedTexture == null)
        {
            lock (_textureLock)
            {
                _sharedTexture ??= _textureProvider.GetTexture("atlas");
            }
            return _sharedTexture;
        }
        return _sharedTexture;
    }

    // Maximum number of chunks to process per frame
    private const int MaxChunksToProcessPerFrame = 5;

    // Flag to indicate if chunk generation is in progress
    private bool _isGenerating;

    // Lock object for generation flag
    private readonly Lock _generationLock = new();

    // Semaphore to limit the number of concurrent chunk generation tasks
    private readonly SemaphoreSlim _chunkGenerationSemaphore = new(Environment.ProcessorCount);

    /// <summary>
    /// Generates initial chunks around a specified origin within a given radius.
    /// </summary>
    public void GenerateInitialChunks(ChunkPosition origin, int radius)
    {
        int size = Chunk.SIZE;

        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                ChunkPosition chunkPosition = new(origin.X + x * size, 0, origin.Z + z * size);

                // Add to active positions and enqueue for generation
                if (_activeChunkPositions.TryAdd(chunkPosition, true))
                {
                    _chunksToGenerate.Enqueue(chunkPosition);
                }
            }
        }

        StartChunkGeneration();
    }

    /// <summary>
    /// Starts processing the chunk generation queue in the background.
    /// </summary>
    private void StartChunkGeneration()
    {
        lock (_generationLock)
        {
            if (!_isGenerating)
            {
                _isGenerating = true;
                Task.Run(async () =>
                {
                    try
                    {
                        await ProcessChunkQueue();
                    }
                    finally
                    {
                        lock (_generationLock)
                        {
                            _isGenerating = false;
                        }
                    }
                });
            }
        }
    }

    /// <summary>
    /// Processes the chunk generation queue asynchronously.
    /// </summary>
    private async Task ProcessChunkQueue()
    {
        while (_chunksToGenerate.TryDequeue(out ChunkPosition chunkPosition))
        {
            // Wait for an available slot in the semaphore
            await _chunkGenerationSemaphore.WaitAsync();

            // Start a new task for chunk data generation
            _ = Task.Run(async () =>
            {
                try
                {
                    await GenerateChunkData(chunkPosition);
                }
                finally
                {
                    // Release the semaphore slot
                    _chunkGenerationSemaphore.Release();
                }
            });
        }
    }

    /// <summary>
    /// Generates chunk data asynchronously.
    /// </summary>
    private async Task GenerateChunkData(ChunkPosition chunkPosition)
    {
        // Check if the chunk already exists
        if (_chunkEntities.ContainsKey(chunkPosition))
        {
            return;
        }

        // Generate chunk mesh data and chunk data
        (ChunkMeshData? meshData, ChunkData? generatedChunkData) = await Task.Run(() =>
        {
            ChunkMeshData mesh = ChunkGenerator.GenerateChunkMesh(
                chunkPosition,
                _worldSystem,
                out ChunkData outChunkData
            );
            return (mesh, outChunkData);
        });

        // Enqueue the chunk for main thread processing
        _chunksAwaitingMainThreadProcessing.Enqueue(
            (chunkPosition, meshData, generatedChunkData)
        );
    }

    /// <summary>
    /// Updates the chunk generation system; should be called once per frame on the main thread.
    /// </summary>
    public void Update()
    {
        int chunksProcessed = 0;

        // Process a limited number of chunks per frame to avoid frame drops
        while (
            chunksProcessed < MaxChunksToProcessPerFrame
            && _chunksAwaitingMainThreadProcessing.TryDequeue(out (ChunkPosition chunkPosition, ChunkMeshData chunkMeshData, ChunkData chunkData) item)
        )
        {
            (ChunkPosition chunkPosition, ChunkMeshData? chunkMeshData, ChunkData? chunkData) = item;
            CreateChunkEntity(chunkPosition, chunkMeshData, chunkData);
            chunksProcessed++;
        }
    }

    /// <summary>
    /// Creates a chunk entity on the main thread with the provided data.
    /// </summary>
    private void CreateChunkEntity(
        ChunkPosition chunkPosition,
        ChunkMeshData chunkMeshData,
        ChunkData chunkData
    )
    {
        // Create a new chunk entity
        int chunkEntity = _entityManager.CreateEntity();

        // Convert chunk position to Vector3
        Vector3 chunkPositionVEC3 = new(chunkPosition.X, chunkPosition.Y, chunkPosition.Z);

        // Add components to the entity
        _entityManager.AddComponent(
            chunkEntity,
            new TransformComponent(chunkPositionVEC3, Quaternion.Identity, Vector3.One)
        );
        _entityManager.AddComponent(
            chunkEntity,
            new MeshComponent(
                chunkMeshData.Vertices,
                chunkMeshData.UVs,
                chunkMeshData.Normals,
                chunkMeshData.Indices
            )
        );
        _entityManager.AddComponent(chunkEntity, new TextureComponent(GetSharedTexture()));

        // Store the chunk entity
        _chunkEntities[chunkPosition] = chunkEntity;

        // Add the chunk to the world system
        _worldSystem.AddChunk(chunkPosition, chunkData);

        // After the chunk is fully generated, update neighboring chunks
        UpdateNeighboringChunks(chunkPosition);
    }

    /// <summary>
    /// Updates the meshes of neighboring chunks when a new chunk is generated
    /// </summary>
    private void UpdateNeighboringChunks(ChunkPosition chunkPosition)
    {
        try
        {
            int size = Chunk.SIZE;
            // Define the relative positions of neighboring chunks
            ReadOnlySpan<ChunkPosition> neighborOffsets =
            [
                new(size, 0, 0), // Right
                new(-size, 0, 0), // Left
                new(0, 0, size), // Front
                new(0, 0, -size), // Back
            ];

            // Create a list to track chunks that need updates
            List<(ChunkPosition, int)> chunksToUpdate = [];

            // First, identify all chunks that need updates
            foreach (ref readonly ChunkPosition offset in neighborOffsets)
            {
                ChunkPosition neighborPos =
                    new(
                        chunkPosition.X + offset.X,
                        chunkPosition.Y,
                        chunkPosition.Z + offset.Z
                    );

                // Check if the neighbor exists and get its entity ID atomically
                if (_chunkEntities.TryGetValue(neighborPos, out int neighborEntity))
                {
                    // Verify the chunk still exists in the world system
                    if (_worldSystem.ChunkExists(neighborPos))
                    {
                        chunksToUpdate.Add((neighborPos, neighborEntity));
                    }
                }
            }

            // Then update all chunks that were valid
            foreach ((ChunkPosition neighborPos, int neighborEntity) in chunksToUpdate)
            {
                try
                {
                    RegenerateChunkMesh(neighborPos, neighborEntity);
                }
                catch (Exception e)
                {
                    Console.WriteLine(
                        $"Failed to regenerate mesh for chunk at {neighborPos}: {e.Message}"
                    );
                    // Continue with other chunks even if one fails
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error during neighbor chunk updates: {e.Message}");
        }
    }

    /// <summary>
    /// Regenerates the mesh for an existing chunk
    /// </summary>
    private void RegenerateChunkMesh(ChunkPosition chunkPosition, int chunkEntity)
    {
        if (!_entityManager.EntityExists(chunkEntity))
        {
            // Entity was removed while we were processing
            return;
        }

        try
        {
            // Get the existing chunk data with a null check
            ChunkData? existingChunkData = _worldSystem.GetChunk(chunkPosition);
            if (existingChunkData == null)
            {
                Console.WriteLine(
                    $"Warning: Chunk data not found for position {chunkPosition}"
                );
                return;
            }

            // Generate new mesh data
            ChunkMeshData? newMeshData = null;
            try
            {
                newMeshData = ChunkGenerator.GenerateChunkMesh(
                    chunkPosition,
                    _worldSystem,
                    out ChunkData _ // Discard the output chunk data since we already have it
                );
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to generate new mesh data: {e.Message}");
                return;
            }

            // Validate mesh data
            if (!ValidateMeshData(newMeshData))
            {
                Console.WriteLine($"Invalid mesh data generated for chunk at {chunkPosition}");
                return;
            }

            // Update the mesh component
            MeshComponent? meshComponent = _entityManager.GetComponent<MeshComponent>(chunkEntity);
            if (meshComponent == null)
            {
                Console.WriteLine($"Warning: MeshComponent not found for entity {chunkEntity}");
                return;
            }

            try
            {
                // Update the mesh data in a thread-safe manner
                lock (meshComponent)
                {
                    // Update the mesh data
                    meshComponent.Vertices = newMeshData.Vertices;
                    meshComponent.UVs = newMeshData.UVs;
                    meshComponent.Normals = newMeshData.Normals;
                    meshComponent.Indices = newMeshData.Indices;

                    // Force buffer recreation on next render
                    meshComponent.ResetBuffers();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to update mesh component: {e.Message}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(
                $"Critical error during mesh regeneration for chunk {chunkPosition}: {e.Message}"
            );
        }
    }

    /// <summary>
    /// Validates that the mesh data is complete and consistent
    /// </summary>
    private static bool ValidateMeshData(ChunkMeshData meshData)
    {
        try
        {
            // Check for null data
            if (
                meshData == null
                || meshData.Vertices == null
                || meshData.UVs == null
                || meshData.Normals == null
                || meshData.Indices == null
            )
            {
                return false;
            }

            // Check for empty collections
            if (
                meshData.Vertices.Count == 0
                || meshData.UVs.Count == 0
                || meshData.Normals.Count == 0
                || meshData.Indices.Count == 0
            )
            {
                return false;
            }

            // Verify data consistency
            if (
                meshData.Vertices.Count != meshData.Normals.Count
                || meshData.Vertices.Count != meshData.UVs.Count
            )
            {
                return false;
            }

            // Verify indices are within bounds
            uint maxIndex = (uint)meshData.Vertices.Count - 1;
            foreach (uint index in meshData.Indices)
            {
                if (index > maxIndex)
                {
                    return false;
                }
            }

            // Verify triangle count is valid (must be multiple of 3)
            if (meshData.Indices.Count % 3 != 0)
            {
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Updates chunks based on the player's position and render distance.
    /// </summary>
    public void UpdateChunks(Vector3 playerPosition, int renderDistance)
    {
        int size = Chunk.SIZE;
        int playerChunkX = (int)Math.Floor(playerPosition.X / size) * size;
        int playerChunkZ = (int)Math.Floor(playerPosition.Z / size) * size;

        ChunkPosition playerChunkPosition = new(playerChunkX, 0, playerChunkZ);

        // Skip the full scan when neither the player's chunk nor the render distance changed
        if (_lastCenter == playerChunkPosition && _lastRenderDistance == renderDistance)
        {
            return;
        }

        _lastCenter = playerChunkPosition;
        _lastRenderDistance = renderDistance;

        ConcurrentDictionary<ChunkPosition, bool> newActiveChunks = [];

        // Determine which chunks should be active
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                int chunkX = playerChunkPosition.X + x * size;
                int chunkZ = playerChunkPosition.Z + z * size;

                ChunkPosition chunkPosition = new(chunkX, 0, chunkZ);
                newActiveChunks.TryAdd(chunkPosition, true);

                // Enqueue chunks that are not already active
                if (!_activeChunkPositions.ContainsKey(chunkPosition))
                {
                    _chunksToGenerate.Enqueue(chunkPosition);
                    _activeChunkPositions.TryAdd(chunkPosition, true);
                }
            }
        }

        // Identify and remove chunks that are no longer within render distance
        List<ChunkPosition> chunksToRemove = [];
        foreach (KeyValuePair<ChunkPosition, bool> chunkPos in _activeChunkPositions)
        {
            if (!newActiveChunks.ContainsKey(chunkPos.Key))
            {
                chunksToRemove.Add(chunkPos.Key);
            }
        }

        foreach (ChunkPosition chunkPos in chunksToRemove)
        {
            RemoveChunk(chunkPos);
            _activeChunkPositions.TryRemove(chunkPos, out _);
        }

        StartChunkGeneration();
    }

    /// <summary>
    /// Removes a chunk entity and its data.
    /// </summary>
    private void RemoveChunk(ChunkPosition chunkPosition)
    {
        if (_chunkEntities.TryRemove(chunkPosition, out int chunkEntity))
        {
            _entityManager.RemoveComponent<TransformComponent>(chunkEntity);
            _entityManager.RemoveComponent<MeshComponent>(chunkEntity);
            _entityManager.RemoveComponent<TextureComponent>(chunkEntity);
            _entityManager.RemoveEntity(chunkEntity);
            _worldSystem.RemoveChunk(
                new ChunkPosition(chunkPosition.X, chunkPosition.Y, chunkPosition.Z)
            );
        }
    }
}

/// <summary>
/// Represents the integer coordinates of a chunk.
/// </summary>
public readonly record struct ChunkPosition(int X, int Y, int Z)
{
    public static ChunkPosition Zero() => new(0, 0, 0);
}
