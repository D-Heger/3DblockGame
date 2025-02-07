using OpenTK.Mathematics;
using VoxelGame.World.Data;

namespace VoxelGame.World
{
    public class Block
    {
        public Vector3 Position;
        public BlockType Type;

        public Dictionary<Faces, FaceData> Faces;

        public Dictionary<Faces, List<Vector2>> textureCoordinateMapping =
            new()
            {
                { Data.Faces.FRONT, new List<Vector2>() },
                { Data.Faces.BACK, new List<Vector2>() },
                { Data.Faces.LEFT, new List<Vector2>() },
                { Data.Faces.RIGHT, new List<Vector2>() },
                { Data.Faces.TOP, new List<Vector2>() },
                { Data.Faces.BOTTOM, new List<Vector2>() },
            };

        public Dictionary<Faces, List<Vector2>> MapCoordinatesToUVs(
            Dictionary<Faces, Vector2> coords
        )
        {
            Dictionary<Faces, List<Vector2>> faceData = [];

            foreach (var faceCoord in coords)
            {
                faceData[faceCoord.Key] =
                [
                    new((faceCoord.Value.X + 1f) / 16f, (faceCoord.Value.Y + 1f) / 16f),
                    new(faceCoord.Value.X / 16f, (faceCoord.Value.Y + 1f) / 16f),
                    new(faceCoord.Value.X / 16f, faceCoord.Value.Y / 16f),
                    new((faceCoord.Value.X + 1f) / 16f, faceCoord.Value.Y / 16f),
                ];
            }

            return faceData;
        }

        public Block(Vector3 position, BlockType blockType = BlockType.AIR)
        {
            Type = blockType;
            Position = position;

            if (blockType != BlockType.AIR)
            {
                textureCoordinateMapping = MapCoordinatesToUVs(
                    TextureData.blockTypeUVCoord[blockType]
                );
            }

            Faces = new Dictionary<Faces, FaceData>
            {
                {
                    Data.Faces.FRONT,
                    new FaceData
                    {
                        Vertices = AddTransformedVertices(
                            FaceDataRaw.rawVertexData[Data.Faces.FRONT]
                        ),
                        TextureCoordinates = textureCoordinateMapping[Data.Faces.FRONT],
                    }
                },
                {
                    Data.Faces.BACK,
                    new FaceData
                    {
                        Vertices = AddTransformedVertices(
                            FaceDataRaw.rawVertexData[Data.Faces.BACK]
                        ),
                        TextureCoordinates = textureCoordinateMapping[Data.Faces.BACK],
                    }
                },
                {
                    Data.Faces.LEFT,
                    new FaceData
                    {
                        Vertices = AddTransformedVertices(
                            FaceDataRaw.rawVertexData[Data.Faces.LEFT]
                        ),
                        TextureCoordinates = textureCoordinateMapping[Data.Faces.LEFT],
                    }
                },
                {
                    Data.Faces.RIGHT,
                    new FaceData
                    {
                        Vertices = AddTransformedVertices(
                            FaceDataRaw.rawVertexData[Data.Faces.RIGHT]
                        ),
                        TextureCoordinates = textureCoordinateMapping[Data.Faces.RIGHT],
                    }
                },
                {
                    Data.Faces.TOP,
                    new FaceData
                    {
                        Vertices = AddTransformedVertices(
                            FaceDataRaw.rawVertexData[Data.Faces.TOP]
                        ),
                        TextureCoordinates = textureCoordinateMapping[Data.Faces.TOP],
                    }
                },
                {
                    Data.Faces.BOTTOM,
                    new FaceData
                    {
                        Vertices = AddTransformedVertices(
                            FaceDataRaw.rawVertexData[Data.Faces.BOTTOM]
                        ),
                        TextureCoordinates = textureCoordinateMapping[Data.Faces.BOTTOM],
                    }
                },
            };
        }

        public List<Vector3> AddTransformedVertices(List<Vector3> vertices)
        {
            List<Vector3> transformedVertices = [];
            foreach (var vert in vertices)
            {
                transformedVertices.Add(vert + Position);
            }
            return transformedVertices;
        }

        public FaceData GetFace(Faces face)
        {
            return Faces[face];
        }
    }
}
