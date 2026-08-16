namespace VoxelGame.World;

/// <summary>
/// Position of a chunk column in chunk-space units (not block coordinates).
/// </summary>
public readonly record struct ChunkPosition(int X, int Z)
{
    public static ChunkPosition Zero => new(0, 0);

    /// <summary>
    /// Converts block/world coordinates to chunk-space (floor division).
    /// </summary>
    public static ChunkPosition FromWorld(int blockX, int blockZ)
        => new(blockX >> 4, blockZ >> 4);

    /// <summary>
    /// Converts float world coordinates to chunk-space. Uses floor rounding —
    /// truncation toward zero would misplace positions in (-1, 0).
    /// </summary>
    public static ChunkPosition FromWorld(float blockX, float blockZ)
        => new((int)MathF.Floor(blockX / Chunk.SIZE), (int)MathF.Floor(blockZ / Chunk.SIZE));

    public int WorldX => X << 4;

    public int WorldZ => Z << 4;
}
