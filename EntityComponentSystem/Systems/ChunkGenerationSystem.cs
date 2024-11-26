using System.Collections.Concurrent;
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
        private readonly EntityManager _entityManager = entityManager;
        private readonly WorldSystem _worldSystem = worldSystem;
        private readonly ConcurrentDictionary<ChunkPosition, int> _chunkEntities = new();
        private readonly ConcurrentQueue<ChunkPosition> _chunksToGenerate = new();
        private readonly HashSet<ChunkPosition> _activeChunkPositions = [];
        private readonly ConcurrentQueue<(ChunkPosition chunkPosition, ChunkMeshData chunkMeshData, ChunkData chunkData)> _chunksAwaitingMainThreadProcessing = new();
        private readonly Texture _sharedTexture = new("atlas");

        public void GenerateInitialChunks(ChunkPosition origin, int radius)
        {
            int size = Chunk.SIZE;
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    ChunkPosition chunkPosition = new(origin.X + x * size, 0, origin.Z + z * size);
                    _chunksToGenerate.Enqueue(chunkPosition);
                    _activeChunkPositions.Add(chunkPosition);
                }
            }

            StartChunkGeneration();
        }

        private void StartChunkGeneration()
        {
            Task.Run(() =>
            {
                while (_chunksToGenerate.TryDequeue(out ChunkPosition chunkPosition))
                {
                    GenerateChunkData(chunkPosition);
                }
            });
        }

        private void GenerateChunkData(ChunkPosition chunkPosition)
        {
            // Check if the chunk already exists
            if (_chunkEntities.ContainsKey(chunkPosition))
            {
                return;
            }

            // Generate chunk mesh data & store the blocks
            Vector3 chunkPositionVector3 = new(chunkPosition.X, chunkPosition.Y, chunkPosition.Z);

            ChunkMeshData chunkMeshData = ChunkGenerator.GenerateChunkMesh(chunkPositionVector3, _worldSystem, out ChunkData chunkData);

            // Enqueue for main thread processing
            _chunksAwaitingMainThreadProcessing.Enqueue((chunkPosition, chunkMeshData, chunkData));
        }

        public void Update()
        {
            // Process chunks that have their data generated and need OpenGL resources
            while (_chunksAwaitingMainThreadProcessing.TryDequeue(out var item))
            {
                var (chunkPosition, chunkMeshData, chunkData) = item;
                CreateChunkEntity(chunkPosition, chunkMeshData, chunkData);
            }
        }

        private void CreateChunkEntity(ChunkPosition chunkPosition, ChunkMeshData chunkMeshData, ChunkData chunkData)
        {
            // Create a new chunk entity
            int chunkEntity = _entityManager.CreateEntity();

            Vector3 chunkPositionVector3 = new(chunkPosition.X, chunkPosition.Y, chunkPosition.Z);

            // Add TransformComponent
            _entityManager.AddComponent(
                chunkEntity,
                new TransformComponent(chunkPositionVector3, Quaternion.Identity, Vector3.One)
            );

            // Add MeshComponent
            _entityManager.AddComponent(
                chunkEntity,
                new MeshComponent(chunkMeshData.Vertices, chunkMeshData.UVs, chunkMeshData.Indices)
            );

            // Add TextureComponent with shared texture
            _entityManager.AddComponent(chunkEntity, new TextureComponent(_sharedTexture));

            // Store the chunk entity
            _chunkEntities[chunkPosition] = chunkEntity;

            // Add the chunk to the world system
            _worldSystem.AddChunk(chunkPositionVector3, chunkData);
        }

        public void UpdateChunks(Vector3 playerPosition, int renderDistance)
        {
            int size = Chunk.SIZE;
            int playerChunkX = (int)(playerPosition.X / size) * size;
            int playerChunkZ = (int)(playerPosition.Z / size) * size;

            ChunkPosition playerChunkPosition = new(playerChunkX, 0, playerChunkZ);

            HashSet<ChunkPosition> newActiveChunks = [];

            // Add new chunks within render distance
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    int chunkX = playerChunkPosition.X + x * size;
                    int chunkZ = playerChunkPosition.Z + z * size;

                    ChunkPosition chunkPosition = new(chunkX, 0, chunkZ);
                    newActiveChunks.Add(chunkPosition);

                    if (!_activeChunkPositions.Contains(chunkPosition))
                    {
                        _chunksToGenerate.Enqueue(chunkPosition);
                        _activeChunkPositions.Add(chunkPosition);
                    }
                }
            }

            // Remove chunks that are no longer in the active set
            foreach (var chunkPos in _activeChunkPositions)
            {
                if (!newActiveChunks.Contains(chunkPos))
                {
                    RemoveChunk(chunkPos);
                }
            }

            _activeChunkPositions.Clear();
            foreach (var chunkPos in newActiveChunks)
            {
                _activeChunkPositions.Add(chunkPos);
            }

            StartChunkGeneration();
        }

        private void RemoveChunk(ChunkPosition chunkPosition)
        {
            if (_chunkEntities.TryGetValue(chunkPosition, out int chunkEntity))
            {
                _entityManager.RemoveEntity(chunkEntity);
                _chunkEntities.TryRemove(chunkPosition, out _);
                _worldSystem.RemoveChunk(new Vector3(chunkPosition.X, chunkPosition.Y, chunkPosition.Z));
            }
        }
    }

    public struct ChunkPosition(int x, int y, int z)
    {
        public int X = x;
        public int Y = y;
        public int Z = z;

        public static ChunkPosition Zero() => new(0, 0, 0);

        public override bool Equals(object obj) =>
            obj is ChunkPosition other &&
            X == other.X &&
            Y == other.Y &&
            Z == other.Z;

        public override readonly int GetHashCode() =>
            HashCode.Combine(X, Y, Z);

        public static bool operator ==(ChunkPosition left, ChunkPosition right) =>
            left.Equals(right);

        public static bool operator !=(ChunkPosition left, ChunkPosition right) =>
            !(left == right);
    }
}
