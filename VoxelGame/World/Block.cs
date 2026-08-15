using OpenTK.Mathematics;
using VoxelGame.World.Data;

namespace VoxelGame.World;

public class Block
{
    public Vector3 Position;
    public BlockType Type;

    public Dictionary<Faces, FaceData> Faces;

    public Block(Vector3 position, BlockType blockType = BlockType.AIR)
    {
        Type = blockType;
        Position = position;

        Faces = new Dictionary<Faces, FaceData>();

        if (blockType != BlockType.AIR)
        {
            for (int faceIdx = 0; faceIdx < 6; faceIdx++)
            {
                Faces face = (Faces)faceIdx;
                Vector2 faceCoord = TextureData.blockTypeUVCoord[(int)blockType, faceIdx];
                Vector2[] textureCoordinates =
                [
                    new((faceCoord.X + 1f) / 16f, (faceCoord.Y + 1f) / 16f),
                    new(faceCoord.X / 16f, (faceCoord.Y + 1f) / 16f),
                    new(faceCoord.X / 16f, faceCoord.Y / 16f),
                    new((faceCoord.X + 1f) / 16f, faceCoord.Y / 16f),
                ];

                Faces[face] = new FaceData
                {
                    Vertices = AddTransformedVertices(FaceDataRaw.rawVertexData[faceIdx]),
                    TextureCoordinates = textureCoordinates,
                };
            }
        }
    }

    public Vector3[] AddTransformedVertices(Vector3[] vertices)
    {
        Vector3[] transformedVertices = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            transformedVertices[i] = vertices[i] + Position;
        }
        return transformedVertices;
    }

    public FaceData GetFace(Faces face)
    {
        return Faces[face];
    }
}
