using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using VoxelGame.EntityComponentSystem;
using VoxelGame.EntityComponentSystem.Components;
using VoxelGame.GraphicsPipeline;
using VoxelGame.World;
using VoxelGame.World.Data;

namespace VoxelGame.EntityComponentSystem.Systems
{
    public class ChunkGenerationSystem(EntityManager entityManager, WorldSystem worldSystem)
        : System
    {
        private readonly EntityManager _entityManager = entityManager;
        private readonly WorldSystem _worldSystem = worldSystem;

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

        // Shared texture for all chunks
        private readonly Texture _sharedTexture = new("atlas");

        // Maximum number of chunks to process per frame
        private const int MaxChunksToProcessPerFrame = 5;

        // Flag to indicate if chunk generation is in progress
        private bool _isGenerating = false;

        // Lock object for generation flag
        private readonly object _generationLock = new();

        // Semaphore to limit the number of concurrent chunk generation tasks
        private readonly SemaphoreSlim _chunkGenerationSemaphore = new SemaphoreSlim(
            Environment.ProcessorCount
        );

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
                return;

            // Generate chunk mesh data and chunk data
            ChunkMeshData chunkMeshData = await Task.Run(
                () =>
                    ChunkGenerator.GenerateChunkMesh(
                        chunkPosition,
                        _worldSystem,
                        out ChunkData chunkData
                    )
            );

            // Enqueue the chunk for main thread processing
            _chunksAwaitingMainThreadProcessing.Enqueue(
                (
                    chunkPosition,
                    chunkMeshData,
                    ChunkGenerator.GenerateChunkData(chunkPosition)
                )
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
                && _chunksAwaitingMainThreadProcessing.TryDequeue(out var item)
            )
            {
                var (chunkPosition, chunkMeshData, chunkData) = item;
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
                new MeshComponent(chunkMeshData.Vertices, chunkMeshData.UVs, chunkMeshData.Indices)
            );
            _entityManager.AddComponent(chunkEntity, new TextureComponent(_sharedTexture));

            // Store the chunk entity
            _chunkEntities[chunkPosition] = chunkEntity;

            // Add the chunk to the world system
            _worldSystem.AddChunk(chunkPosition, chunkData);
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
            var chunksToRemove = new List<ChunkPosition>();
            foreach (var chunkPos in _activeChunkPositions)
            {
                if (!newActiveChunks.ContainsKey(chunkPos.Key))
                {
                    chunksToRemove.Add(chunkPos.Key);
                }
            }

            foreach (var chunkPos in chunksToRemove)
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
    public readonly struct ChunkPosition(int x, int y, int z) : IEquatable<ChunkPosition>
    {
        public readonly int X = x;
        public readonly int Y = y;
        public readonly int Z = z;

        public static ChunkPosition Zero() => new(0, 0, 0);

        public readonly bool Equals(ChunkPosition other) =>
            X == other.X && Y == other.Y && Z == other.Z;

        public override bool Equals(object obj) => obj is ChunkPosition other && Equals(other);

        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);

        public static bool operator ==(ChunkPosition left, ChunkPosition right) =>
            left.Equals(right);

        public static bool operator !=(ChunkPosition left, ChunkPosition right) =>
            !left.Equals(right);
    }
}
