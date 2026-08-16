using VoxelGame.World.Data;

namespace VoxelGame.World;

/// <summary>
/// Block storage for one chunk column, split into <see cref="SectionCount"/>
/// lazily allocated 16×16×16 sections. A null section is all air, so untouched
/// chunks cost almost nothing.
/// </summary>
public sealed class ChunkData
{
    public const int SectionCount = Chunk.HEIGHT / ChunkSection.Size;

    public ChunkSection?[] Sections { get; } = new ChunkSection?[SectionCount];

    public int UsedSectionCount
    {
        get
        {
            int count = 0;
            foreach (ChunkSection? section in Sections)
            {
                if (section != null)
                {
                    count++;
                }
            }
            return count;
        }
    }

    public BlockType GetBlock(int x, int y, int z)
    {
        ChunkSection? section = Sections[y >> 4];
        return section == null ? BlockType.AIR : section.GetBlock(x, y & (ChunkSection.Size - 1), z);
    }

    public void SetBlock(int x, int y, int z, BlockType type)
    {
        int sectionY = y >> 4;
        ChunkSection? section = Sections[sectionY];
        if (section == null)
        {
            // Lock-free lazy allocation; concurrent generation threads may race
            // here, Interlocked.CompareExchange picks a single winning section.
            ChunkSection newSection = new();
            section = Interlocked.CompareExchange(ref Sections[sectionY], newSection, null) ?? newSection;
        }
        section.SetBlock(x, y & (ChunkSection.Size - 1), z, type);
    }

    public void Fill(BlockType type)
    {
        if (type == BlockType.AIR)
        {
            Array.Clear(Sections);
            return;
        }

        for (int i = 0; i < SectionCount; i++)
        {
            ChunkSection section = Sections[i] ??= new ChunkSection();
            Array.Fill(section.Blocks, type);
        }
    }
}
