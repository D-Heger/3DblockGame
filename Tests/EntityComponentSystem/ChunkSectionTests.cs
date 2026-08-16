using VoxelGame.World;
using VoxelGame.World.Data;

namespace Tests.EntityComponentSystem;

public class ChunkSectionTests
{
    [Fact]
    public void NewSection_IsAllAir()
    {
        ChunkSection section = new();

        for (int x = 0; x < ChunkSection.Size; x += 5)
        {
            for (int y = 0; y < ChunkSection.Size; y += 5)
            {
                for (int z = 0; z < ChunkSection.Size; z += 5)
                {
                    Assert.Equal(BlockType.AIR, section.GetBlock(x, y, z));
                }
            }
        }
    }

    [Fact]
    public void SetBlock_AndGetBlock_RoundTrips()
    {
        ChunkSection section = new();

        section.SetBlock(3, 9, 11, BlockType.STONE);

        Assert.Equal(BlockType.STONE, section.GetBlock(3, 9, 11));
    }

    [Fact]
    public void SetBlock_AllCorners_RoundTrips()
    {
        ChunkSection section = new();
        (int x, int y, int z)[] positions =
        [
            (0, 0, 0),
            (ChunkSection.Size - 1, 0, 0),
            (0, ChunkSection.Size - 1, 0),
            (0, 0, ChunkSection.Size - 1),
            (ChunkSection.Size - 1, ChunkSection.Size - 1, ChunkSection.Size - 1),
        ];

        foreach ((int x, int y, int z) in positions)
        {
            section.SetBlock(x, y, z, BlockType.GRASS);
        }

        foreach ((int x, int y, int z) in positions)
        {
            Assert.Equal(BlockType.GRASS, section.GetBlock(x, y, z));
        }
    }
}
