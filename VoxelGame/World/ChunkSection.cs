using VoxelGame.World.Data;

namespace VoxelGame.World;

/// <summary>
/// A 16×16×16 cube of blocks within a chunk column. Plain data only, kept
/// serialize-friendly: the future save unit is a section bitmask plus the raw
/// block bytes. A null section reference in <see cref="ChunkData"/> means the
/// whole section is air.
/// </summary>
public sealed class ChunkSection
{
    public const int Size = Chunk.SIZE;
    public const int BlockCount = Size * Size * Size;

    public readonly BlockType[] Blocks = new BlockType[BlockCount];

    public BlockType GetBlock(int x, int y, int z) => Blocks[(x << 8) | (y << 4) | z];

    public void SetBlock(int x, int y, int z, BlockType type) => Blocks[(x << 8) | (y << 4) | z] = type;
}
