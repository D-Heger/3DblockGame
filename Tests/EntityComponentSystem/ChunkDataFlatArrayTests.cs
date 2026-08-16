using VoxelGame.EntityComponentSystem.Systems;
using VoxelGame.World;
using VoxelGame.World.Data;

namespace Tests.EntityComponentSystem;

public class ChunkDataFlatArrayTests
{
    [Fact]
    public void GetBlock_ReturnsDefaultValue()
    {
        ChunkData data = new(Chunk.SIZE, Chunk.HEIGHT, Chunk.SIZE);
        Assert.Equal(BlockType.DIRT, data.GetBlock(0, 0, 0));
        Assert.Equal(BlockType.DIRT, data.GetBlock(15, 383, 15));
    }

    [Fact]
    public void SetBlock_AndGetBlock_RoundTrips()
    {
        ChunkData data = new(Chunk.SIZE, Chunk.HEIGHT, Chunk.SIZE);
        data.SetBlock(5, 100, 7, BlockType.STONE);
        Assert.Equal(BlockType.STONE, data.GetBlock(5, 100, 7));
    }

    [Fact]
    public void SetBlock_AllCorners_RoundTrips()
    {
        ChunkData data = new(Chunk.SIZE, Chunk.HEIGHT, Chunk.SIZE);
        (int x, int y, int z)[] positions =
        [
            (0, 0, 0),
            (Chunk.SIZE - 1, 0, 0),
            (0, Chunk.HEIGHT - 1, 0),
            (0, 0, Chunk.SIZE - 1),
            (Chunk.SIZE - 1, Chunk.HEIGHT - 1, Chunk.SIZE - 1),
        ];

        foreach ((int x, int y, int z) in positions)
        {
            data.SetBlock(x, y, z, BlockType.GRASS);
        }

        foreach ((int x, int y, int z) in positions)
        {
            Assert.Equal(BlockType.GRASS, data.GetBlock(x, y, z));
        }
    }

    [Fact]
    public void FlatArray_HasCorrectSize()
    {
        ChunkData data = new(Chunk.SIZE, Chunk.HEIGHT, Chunk.SIZE);
        Assert.Equal(Chunk.SIZE * Chunk.HEIGHT * Chunk.SIZE, data.Blocks.Length);
    }

    [Fact]
    public void SetBlock_DifferentPositions_DoNotOverlap()
    {
        ChunkData data = new(Chunk.SIZE, Chunk.HEIGHT, Chunk.SIZE);
        data.SetBlock(0, 0, 0, BlockType.STONE);
        data.SetBlock(1, 0, 0, BlockType.DIRT);
        data.SetBlock(0, 1, 0, BlockType.GRASS);
        data.SetBlock(0, 0, 1, BlockType.SAND);

        Assert.Equal(BlockType.STONE, data.GetBlock(0, 0, 0));
        Assert.Equal(BlockType.DIRT, data.GetBlock(1, 0, 0));
        Assert.Equal(BlockType.GRASS, data.GetBlock(0, 1, 0));
        Assert.Equal(BlockType.SAND, data.GetBlock(0, 0, 1));
    }
}
