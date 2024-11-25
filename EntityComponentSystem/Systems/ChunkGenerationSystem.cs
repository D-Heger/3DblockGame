using OpenTK.Mathematics;
using VoxelGame.EntityComponentSystem.Components;
using VoxelGame.GraphicsPipeline;
using VoxelGame.World;
using VoxelGame.World.Data;

namespace VoxelGame.EntityComponentSystem.Systems
{
    public class ChunkGenerationSystem : System
    {
        private EntityManager _entityManager;
        private Dictionary<Vector3, int> _chunkEntities;
        private HashSet<Vector3> _activeChunkPositions;

        public ChunkGenerationSystem(EntityManager entityManager)
        {
            _entityManager = entityManager;
            _chunkEntities = [];
            _activeChunkPositions = [];
        }

        public void GenerateInitialChunks(Vector3 origin, int radius)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    Vector3 chunkPosition = new Vector3(
                        x * Chunk.SIZE,
                        0,
                        z * Chunk.SIZE
                    );
                    GenerateChunk(chunkPosition);
                }
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

            // Generate chunk mesh data asynchronously to avoid blocking the main thread
            ChunkMeshData chunkMeshData = ChunkGenerator.GenerateChunkMesh(chunkPosition);

            // Add MeshComponent
            _entityManager.AddComponent(
                chunkEntity,
                new MeshComponent(chunkMeshData.Vertices, chunkMeshData.UVs, chunkMeshData.Indices)
            );

            // Add TextureComponent
            Texture chunkTexture = new Texture("atlas");
            _entityManager.AddComponent(chunkEntity, new TextureComponent(chunkTexture));

            // Store the chunk entity
            _chunkEntities[chunkPosition] = chunkEntity;
            _activeChunkPositions.Add(chunkPosition);
        }

        public void UpdateChunks(Vector3 playerPosition, int renderDistance)
        {
            Vector3 playerChunkPosition = new Vector3(
                (int)(playerPosition.X / Chunk.SIZE) * Chunk.SIZE,
                0,
                (int)(playerPosition.Z / Chunk.SIZE) * Chunk.SIZE
            );

            List<Vector3> chunksToRemove = new List<Vector3>();

            // Remove chunks that are out of range
            foreach (var chunkPos in _activeChunkPositions)
            {
                if (
                    Vector3.Distance(chunkPos, playerChunkPosition)
                    > renderDistance * Chunk.SIZE
                )
                {
                    chunksToRemove.Add(chunkPos);
                }
            }

            foreach (var chunkPos in chunksToRemove)
            {
                RemoveChunk(chunkPos);
            }

            // Generate new chunks within render distance using a spatial hashing approach
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    Vector3 chunkPosition =
                        playerChunkPosition
                        + new Vector3(x * Chunk.SIZE, 0, z * Chunk.SIZE);
                    if (!_activeChunkPositions.Contains(chunkPosition))
                    {
                        GenerateChunk(chunkPosition);
                    }
                }
            }
        }

        private void RemoveChunk(Vector3 chunkPosition)
        {
            if (_chunkEntities.TryGetValue(chunkPosition, out int chunkEntity))
            {
                _entityManager.RemoveEntity(chunkEntity);
                _chunkEntities.Remove(chunkPosition);
                _activeChunkPositions.Remove(chunkPosition);
            }
        }
    }
}
