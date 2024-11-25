using OpenTK.Mathematics;

namespace VoxelGame.World.Data
{
    public static class TextureData
    {
        public static readonly Dictionary<BlockType, Dictionary<Faces, Vector2>> blockTypeUVCoord =
            new()
            {
                {
                    BlockType.DIRT,
                    new Dictionary<Faces, Vector2>()
                    {
                        { Faces.FRONT, new Vector2(2f, 15f) },
                        { Faces.LEFT, new Vector2(2f, 15f) },
                        { Faces.RIGHT, new Vector2(2f, 15f) },
                        { Faces.BACK, new Vector2(2f, 15f) },
                        { Faces.TOP, new Vector2(2f, 15f) },
                        { Faces.BOTTOM, new Vector2(2f, 15f) },
                    }
                },
                {
                    BlockType.GRASS,
                    new Dictionary<Faces, Vector2>()
                    {
                        { Faces.FRONT, new Vector2(3f, 15f) },
                        { Faces.LEFT, new Vector2(3f, 15f) },
                        { Faces.RIGHT, new Vector2(3f, 15f) },
                        { Faces.BACK, new Vector2(3f, 15f) },
                        { Faces.TOP, new Vector2(7f, 13f) },
                        { Faces.BOTTOM, new Vector2(3f, 15f) },
                    }
                },
                {
                    BlockType.STONE,
                    new Dictionary<Faces, Vector2>()
                    {
                        { Faces.FRONT, new Vector2(1f, 15f) },
                        { Faces.LEFT, new Vector2(1f, 15f) },
                        { Faces.RIGHT, new Vector2(1f, 15f) },
                        { Faces.BACK, new Vector2(1f, 15f) },
                        { Faces.TOP, new Vector2(1f, 15f) },
                        { Faces.BOTTOM, new Vector2(1f, 15f) },
                    }
                },
                {
                    BlockType.SAND,
                    new Dictionary<Faces, Vector2>()
                    {
                        { Faces.FRONT, new Vector2(2f, 14f) },
                        { Faces.LEFT, new Vector2(2f, 14f) },
                        { Faces.RIGHT, new Vector2(2f, 14f) },
                        { Faces.BACK, new Vector2(2f, 14f) },
                        { Faces.TOP, new Vector2(2f, 14f) },
                        { Faces.BOTTOM, new Vector2(2f, 14f) },
                    }
                },
                {
                    BlockType.WATER,
                    new Dictionary<Faces, Vector2>()
                    {
                        { Faces.FRONT, new Vector2(13f, 3f) },
                        { Faces.LEFT, new Vector2(13f, 3f) },
                        { Faces.RIGHT, new Vector2(13f, 3f) },
                        { Faces.BACK, new Vector2(13f, 3f) },
                        { Faces.TOP, new Vector2(13f, 3f) },
                        { Faces.BOTTOM, new Vector2(13f, 3f) },
                    }
                },
            };

        public static List<Vector2> GetUVs(BlockType blockType, Faces face)
        {
            Vector2 faceCoord = blockTypeUVCoord[blockType][face];
            return
            [
                new Vector2((faceCoord.X + 1f) / 16f, (faceCoord.Y + 1f) / 16f),
                new Vector2(faceCoord.X / 16f, (faceCoord.Y + 1f) / 16f),
                new Vector2(faceCoord.X / 16f, faceCoord.Y / 16f),
                new Vector2((faceCoord.X + 1f) / 16f, faceCoord.Y / 16f),
            ];
        }
    }
}
