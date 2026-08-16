using VoxelGame.World;
using VoxelGame.World.Data;

namespace Tests.EntityComponentSystem;

public class ChunkDataTests
{
    [Fact]
    public void GetBlock_DefaultsToAir()
    {
        ChunkData data = new();

        Assert.Equal(BlockType.AIR, data.GetBlock(0, 0, 0));
        Assert.Equal(BlockType.AIR, data.GetBlock(15, 383, 15));
        Assert.Equal(BlockType.AIR, data.GetBlock(8, 200, 3));
    }

    [Fact]
    public void NewChunk_HasNoAllocatedSections()
    {
        ChunkData data = new();

        Assert.Equal(0, data.UsedSectionCount);
    }

    [Fact]
    public void SetBlock_AndGetBlock_RoundTrips()
    {
        ChunkData data = new();

        data.SetBlock(5, 100, 7, BlockType.STONE);

        Assert.Equal(BlockType.STONE, data.GetBlock(5, 100, 7));
    }

    [Fact]
    public void SetBlock_AllCorners_RoundTrips()
    {
        ChunkData data = new();
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
    public void SetBlock_DifferentPositions_DoNotOverlap()
    {
        ChunkData data = new();

        data.SetBlock(0, 0, 0, BlockType.STONE);
        data.SetBlock(1, 0, 0, BlockType.DIRT);
        data.SetBlock(0, 1, 0, BlockType.GRASS);
        data.SetBlock(0, 0, 1, BlockType.SAND);

        Assert.Equal(BlockType.STONE, data.GetBlock(0, 0, 0));
        Assert.Equal(BlockType.DIRT, data.GetBlock(1, 0, 0));
        Assert.Equal(BlockType.GRASS, data.GetBlock(0, 1, 0));
        Assert.Equal(BlockType.SAND, data.GetBlock(0, 0, 1));
    }

    [Fact]
    public void LowYWrites_AllocateSingleSection()
    {
        ChunkData data = new();

        data.SetBlock(0, 0, 0, BlockType.STONE);
        data.SetBlock(15, 15, 15, BlockType.DIRT);

        Assert.Equal(1, data.UsedSectionCount);
    }

    [Fact]
    public void WritesAcrossSections_AllocateOnlyTouchedSections()
    {
        ChunkData data = new();

        data.SetBlock(0, 0, 0, BlockType.STONE);
        data.SetBlock(0, Chunk.HEIGHT - 1, 0, BlockType.GRASS);

        Assert.Equal(2, data.UsedSectionCount);
    }

    [Fact]
    public void Fill_SolidifiesEverySection()
    {
        ChunkData data = new();

        data.Fill(BlockType.DIRT);

        Assert.Equal(ChunkData.SectionCount, data.UsedSectionCount);
        Assert.Equal(BlockType.DIRT, data.GetBlock(0, 0, 0));
        Assert.Equal(BlockType.DIRT, data.GetBlock(15, 383, 15));
    }

    [Fact]
    public void Fill_WithAir_ReleasesSections()
    {
        ChunkData data = new();
        data.Fill(BlockType.DIRT);

        data.Fill(BlockType.AIR);

        Assert.Equal(0, data.UsedSectionCount);
        Assert.Equal(BlockType.AIR, data.GetBlock(8, 100, 8));
    }
}
