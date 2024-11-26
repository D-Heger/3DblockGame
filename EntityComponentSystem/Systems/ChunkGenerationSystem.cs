using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using VoxelGame.EntityComponentSystem;
using VoxelGame.EntityComponentSystem.Components;
using VoxelGame.GraphicsPipeline;
using VoxelGame.World;
using VoxelGame.World.Data;

namespace VoxelGame.EntityComponentSystem.Systems
{
    public class ChunkGenerationSystem(EntityManager entityManager, WorldSystem worldSystem) : System
    {
        private EntityManager _entityManager = entityManager;
        private WorldSystem _worldSystem = worldSystem;
        private readonly ConcurrentDictionary<Vector3, int> _chunkEntities = new();
        private readonly ConcurrentQueue<Vector3> _chunksToGenerate = new();
        private readonly HashSet<Vector3> _activeChunkPositions = [];
        private readonly Texture _sharedTexture = new("atlas");

        public void GenerateInitialChunks(Vector3 origin, int radius)
        {
            int size = Chunk.SIZE;
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    Vector3 chunkPosition = new(x * size, 0, z * size);
                    _chunksToGenerate.Enqueue(chunkPosition);
                }
            }

            ProcessChunkQueue();
        }

        private void ProcessChunkQueue()
        {
            while (_chunksToGenerate.TryDequeue(out Vector3 chunkPosition))
            {
                // Chunk generation should be scheduled to occur on the main thread
                // because OpenGL resources must be created and accessed only on the main thread.
                GenerateChunk(chunkPosition);
            }
        }

        public void GenerateChunk(Vector3 chunkPosition)
        {
            // Check if the chunk already exists
            if (_chunkEntities.ContainsKey(chunkPosition))
            {
                return;
            }

            // Create a new chunk entity
            int chunkEntity = _entityManager.CreateEntity();

            // Add TransformComponent
            _entityManager.AddComponent(
                chunkEntity,
                new TransformComponent(chunkPosition, Quaternion.Identity, Vector3.One)
            );

            // Generate chunk mesh data & store the blocks
            ChunkMeshData chunkMeshData = ChunkGenerator.GenerateChunkMesh(chunkPosition, _worldSystem, out ChunkData chunkData);

            // Add MeshComponent
            _entityManager.AddComponent(
                chunkEntity,
                new MeshComponent(chunkMeshData.Vertices, chunkMeshData.UVs, chunkMeshData.Indices)
            );

            // Add TextureComponent with shared texture
            _entityManager.AddComponent(chunkEntity, new TextureComponent(_sharedTexture));

            // Store the chunk entity
            _chunkEntities[chunkPosition] = chunkEntity;
            _activeChunkPositions.Add(chunkPosition);

            // Add the chunk to the world system
            _worldSystem.AddChunk(chunkPosition, chunkData);
        }

        public void UpdateChunks(Vector3 playerPosition, int renderDistance)
        {
            int size = Chunk.SIZE;
            Vector3 playerChunkPosition =
                new(
                    (int)(playerPosition.X / size) * size,
                    0,
                    (int)(playerPosition.Z / size) * size
                );

            List<Vector3> chunksToRemove = [];

            // Remove chunks that are out of range
            foreach (var chunkPos in _activeChunkPositions)
            {
                if (Vector3.Distance(chunkPos, playerChunkPosition) > renderDistance * size)
                {
                    chunksToRemove.Add(chunkPos);
                }
            }

            foreach (var chunkPos in chunksToRemove)
            {
                RemoveChunk(chunkPos);
            }

            // Add new chunks within render distance
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    Vector3 chunkPosition =
                        playerChunkPosition + new Vector3(x * size, 0, z * size);

                    if (!_activeChunkPositions.Contains(chunkPosition))
                    {
                        _chunksToGenerate.Enqueue(chunkPosition);
                    }
                }
            }

            ProcessChunkQueue();
        }

        private void RemoveChunk(Vector3 chunkPosition)
        {
            if (_chunkEntities.TryGetValue(chunkPosition, out int chunkEntity))
            {
                _entityManager.RemoveEntity(chunkEntity);
                _chunkEntities.TryRemove(chunkPosition, out _);
                _activeChunkPositions.Remove(chunkPosition);
                _worldSystem.RemoveChunk(chunkPosition);
            }
        }
    }
}
