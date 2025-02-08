using VoxelGame.EntityComponentSystem.Systems;
using VoxelGame.World;
using VoxelGame.World.Data;

namespace Tests;

public class WorldSystemTests : IDisposable
{
    private readonly WorldSystem _worldSystem;

    public WorldSystemTests()
    {
        _worldSystem = new WorldSystem();
    }

    public void Dispose()
    {
        // Nothing to dispose currently
    }

    [Fact]
    public void AddChunk_StoresChunkData()
    {
        var position = new ChunkPosition(0, 0, 0);
        var blocks = new BlockType[Chunk.SIZE, Chunk.HEIGHT, Chunk.SIZE];
        var chunkData = new ChunkData(blocks);

        _worldSystem.AddChunk(position, chunkData);

        Assert.True(_worldSystem.ChunkExists(position));
        Assert.Same(chunkData, _worldSystem.GetChunk(position));
    }

    [Fact]
    public void RemoveChunk_RemovesChunkData()
    {
        var position = new ChunkPosition(0, 0, 0);
        var blocks = new BlockType[Chunk.SIZE, Chunk.HEIGHT, Chunk.SIZE];
        _worldSystem.AddChunk(position, new ChunkData(blocks));

        _worldSystem.RemoveChunk(position);

        Assert.False(_worldSystem.ChunkExists(position));
        Assert.Null(_worldSystem.GetChunk(position));
    }

    [Fact]
    public void GetAllChunkPositions_ReturnsCorrectPositions()
    {
        var position1 = new ChunkPosition(0, 0, 0);
        var position2 = new ChunkPosition(16, 0, 0);
        var blocks = new BlockType[Chunk.SIZE, Chunk.HEIGHT, Chunk.SIZE];
        
        _worldSystem.AddChunk(position1, new ChunkData(blocks));
        _worldSystem.AddChunk(position2, new ChunkData(blocks));

        var positions = _worldSystem.GetAllChunkPositions().ToList();

        Assert.Equal(2, positions.Count);
        Assert.Contains(position1, positions);
        Assert.Contains(position2, positions);
    }

    [Fact]
    public void GetChunk_NonExistentPosition_ReturnsNull()
    {
        var position = new ChunkPosition(0, 0, 0);

        var chunkData = _worldSystem.GetChunk(position);

        Assert.Null(chunkData);
    }

    [Fact]
    public void AddChunk_UpdatesExistingChunk()
    {
        var position = new ChunkPosition(0, 0, 0);
        var blocks1 = new BlockType[Chunk.SIZE, Chunk.HEIGHT, Chunk.SIZE];
        var blocks2 = new BlockType[Chunk.SIZE, Chunk.HEIGHT, Chunk.SIZE];
        var chunkData1 = new ChunkData(blocks1);
        var chunkData2 = new ChunkData(blocks2);

        _worldSystem.AddChunk(position, chunkData1);
        _worldSystem.AddChunk(position, chunkData2);

        Assert.Same(chunkData2, _worldSystem.GetChunk(position));
    }
}