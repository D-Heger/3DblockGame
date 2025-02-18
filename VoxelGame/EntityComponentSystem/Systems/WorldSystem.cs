using System.Collections.Concurrent;
using OpenTK.Mathematics;
using VoxelGame.World.Data;

namespace VoxelGame.EntityComponentSystem.Systems
{
    public class WorldSystem : System
    {
        private readonly ConcurrentDictionary<ChunkPosition, ChunkData> _activeChunks;

        public WorldSystem()
        {
            _activeChunks = new ConcurrentDictionary<ChunkPosition, ChunkData>();
        }

        public void AddChunk(ChunkPosition chunkPosition, ChunkData chunkData)
        {
            _activeChunks[chunkPosition] = chunkData;
        }

        public void RemoveChunk(ChunkPosition chunkPosition)
        {
            _activeChunks.TryRemove(chunkPosition, out _);
        }

        public bool ChunkExists(ChunkPosition chunkPosition)
        {
            return _activeChunks.ContainsKey(chunkPosition);
        }

        public ChunkData? GetChunk(ChunkPosition chunkPosition)
        {
            _activeChunks.TryGetValue(chunkPosition, out ChunkData? chunkData);
            return chunkData;
        }

        public IEnumerable<ChunkPosition> GetAllChunkPositions()
        {
            return _activeChunks.Keys;
        }
    }

    public class ChunkData(BlockType[,,] blocks)
    {
        public BlockType[,,] Blocks = blocks;
    }
}
