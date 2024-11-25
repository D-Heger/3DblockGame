using System.Collections.Concurrent;
using OpenTK.Mathematics;
using VoxelGame.World.Data;

namespace VoxelGame.EntityComponentSystem.Systems
{
    public class WorldSystem : System
    {
        private readonly ConcurrentDictionary<Vector3, ChunkData> _activeChunks;

        public WorldSystem()
        {
            _activeChunks = new ConcurrentDictionary<Vector3, ChunkData>();
        }

        public void AddChunk(Vector3 chunkPosition, ChunkData chunkData)
        {
            _activeChunks[chunkPosition] = chunkData;
        }

        public void RemoveChunk(Vector3 chunkPosition)
        {
            _activeChunks.TryRemove(chunkPosition, out _);
        }

        public bool ChunkExists(Vector3 chunkPosition)
        {
            return _activeChunks.ContainsKey(chunkPosition);
        }

        public ChunkData GetChunk(Vector3 chunkPosition)
        {
            _activeChunks.TryGetValue(chunkPosition, out ChunkData chunkData);
            return chunkData;
        }

        public IEnumerable<Vector3> GetAllChunkPositions()
        {
            return _activeChunks.Keys;
        }
    }

    public class ChunkData(BlockType[,,] blocks)
    {
        public BlockType[,,] Blocks = blocks;
    }
}