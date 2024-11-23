using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

public class Block
{
    public Vector3 Position;
    public BlockType Type;

    public Dictionary<Faces, FaceData> Faces;

    public Dictionary<Faces, List<Vector2>> textureCoordinateMapping = new()
    {
        { global::Faces.FRONT, new List<Vector2>() },
        { global::Faces.BACK, new List<Vector2>() },
        { global::Faces.LEFT, new List<Vector2>() },
        { global::Faces.RIGHT, new List<Vector2>() },
        { global::Faces.TOP, new List<Vector2>() },
        { global::Faces.BOTTOM, new List<Vector2>() },
    };

    public Dictionary<Faces, List<Vector2>> MapCoordinatesToUVs(Dictionary<Faces, Vector2> coords)
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
            textureCoordinateMapping = MapCoordinatesToUVs(TextureData.blockTypeUVCoord[blockType]);
        }

        Faces = new Dictionary<Faces, FaceData>
        {
            {
                global::Faces.FRONT,
                new FaceData
                {
                    Vertices = AddTransformedVertices(FaceDataRaw.rawVertexData[global::Faces.FRONT]),
                    TextureCoordinates = textureCoordinateMapping[global::Faces.FRONT],
                }
            },
            {
                global::Faces.BACK,
                new FaceData
                {
                    Vertices = AddTransformedVertices(FaceDataRaw.rawVertexData[global::Faces.BACK]),
                    TextureCoordinates = textureCoordinateMapping[global::Faces.BACK],
                }
            },
            {
                global::Faces.LEFT,
                new FaceData
                {
                    Vertices = AddTransformedVertices(FaceDataRaw.rawVertexData[global::Faces.LEFT]),
                    TextureCoordinates = textureCoordinateMapping[global::Faces.LEFT],
                }
            },
            {
                global::Faces.RIGHT,
                new FaceData
                {
                    Vertices = AddTransformedVertices(FaceDataRaw.rawVertexData[global::Faces.RIGHT]),
                    TextureCoordinates = textureCoordinateMapping[global::Faces.RIGHT],
                }
            },
            {
                global::Faces.TOP,
                new FaceData
                {
                    Vertices = AddTransformedVertices(FaceDataRaw.rawVertexData[global::Faces.TOP]),
                    TextureCoordinates = textureCoordinateMapping[global::Faces.TOP],
                }
            },
            {
                global::Faces.BOTTOM,
                new FaceData
                {
                    Vertices = AddTransformedVertices(FaceDataRaw.rawVertexData[global::Faces.BOTTOM]),
                    TextureCoordinates = textureCoordinateMapping[global::Faces.BOTTOM],
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
