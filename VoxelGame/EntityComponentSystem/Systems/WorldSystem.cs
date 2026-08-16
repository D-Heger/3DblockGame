using System.Collections.Concurrent;
using VoxelGame.World;

namespace VoxelGame.EntityComponentSystem.Systems;

public class WorldSystem : System, IChunkSource
{
    private readonly ConcurrentDictionary<ChunkPosition, ChunkData> _activeChunks = new();

    public void AddChunk(ChunkPosition chunkPosition, ChunkData chunkData) => _activeChunks[chunkPosition] = chunkData;

    public void RemoveChunk(ChunkPosition chunkPosition) => _activeChunks.TryRemove(chunkPosition, out _);

    public bool ChunkExists(ChunkPosition chunkPosition) => _activeChunks.ContainsKey(chunkPosition);

    public ChunkData? GetChunk(ChunkPosition chunkPosition)
    {
        _activeChunks.TryGetValue(chunkPosition, out ChunkData? chunkData);
        return chunkData;
    }

    public IEnumerable<ChunkPosition> GetAllChunkPositions() => _activeChunks.Keys;
}
