using System.Collections.Concurrent;
using VoxelGame.World.Data;

namespace VoxelGame.EntityComponentSystem.Systems;

public class WorldSystem : System
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

public class ChunkData
{
    public BlockType[] Blocks;
    public readonly int SizeX;
    public readonly int SizeY;
    public readonly int SizeZ;

    public ChunkData(BlockType[] blocks, int sizeX, int sizeY, int sizeZ)
    {
        Blocks = blocks;
        SizeX = sizeX;
        SizeY = sizeY;
        SizeZ = sizeZ;
    }

    public ChunkData(int sizeX, int sizeY, int sizeZ)
    {
        SizeX = sizeX;
        SizeY = sizeY;
        SizeZ = sizeZ;
        Blocks = new BlockType[sizeX * sizeY * sizeZ];
    }

    public BlockType GetBlock(int x, int y, int z) => Blocks[x * SizeY * SizeZ + y * SizeZ + z];

    public void SetBlock(int x, int y, int z, BlockType type) => Blocks[x * SizeY * SizeZ + y * SizeZ + z] = type;
}
