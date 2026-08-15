using OpenTK.Mathematics;

namespace VoxelGame.World.Data;

public static class TextureData
{
    public static readonly Vector2[,] blockTypeUVCoord = new Vector2[7, 6]
    {
        { new(2f, 15f), new(2f, 15f), new(2f, 15f), new(2f, 15f), new(2f, 15f), new(2f, 15f) },
        { new(3f, 15f), new(3f, 15f), new(3f, 15f), new(3f, 15f), new(7f, 13f), new(2f, 15f) },
        { new(0f, 0f), new(0f, 0f), new(0f, 0f), new(0f, 0f), new(0f, 0f), new(0f, 0f) },
        { new(1f, 15f), new(1f, 15f), new(1f, 15f), new(1f, 15f), new(1f, 15f), new(1f, 15f) },
        { new(2f, 14f), new(2f, 14f), new(2f, 14f), new(2f, 14f), new(2f, 14f), new(2f, 14f) },
        { new(13f, 3f), new(13f, 3f), new(13f, 3f), new(13f, 3f), new(13f, 3f), new(13f, 3f) },
        { new(1f, 14f), new(1f, 14f), new(1f, 14f), new(1f, 14f), new(1f, 14f), new(1f, 14f) },
    };

    private static readonly Vector2[] _precomputedUVs = new Vector2[7 * 6 * 4];

    static TextureData()
    {
        for (int bt = 0; bt < 7; bt++)
        {
            for (int f = 0; f < 6; f++)
            {
                Vector2 faceCoord = blockTypeUVCoord[bt, f];
                int offset = bt * 24 + f * 4;
                _precomputedUVs[offset] = new((faceCoord.X + 1f) / 16f, (faceCoord.Y + 1f) / 16f);
                _precomputedUVs[offset + 1] = new(faceCoord.X / 16f, (faceCoord.Y + 1f) / 16f);
                _precomputedUVs[offset + 2] = new(faceCoord.X / 16f, faceCoord.Y / 16f);
                _precomputedUVs[offset + 3] = new((faceCoord.X + 1f) / 16f, faceCoord.Y / 16f);
            }
        }
    }

    public static ReadOnlySpan<Vector2> GetUVsSpan(BlockType blockType, Faces face)
    {
        return _precomputedUVs.AsSpan((int)blockType * 24 + (int)face * 4, 4);
    }
}
