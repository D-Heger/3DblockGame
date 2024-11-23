using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

public enum BlockType
{
    DIRT,
    GRASS,
    AIR,
}

public enum Faces
{
    FRONT,
    BACK,
    LEFT,
    RIGHT,
    TOP,
    BOTTOM,
}

public struct FaceData
{
    public List<Vector3> Vertices;
    public List<Vector2> TextureCoordinates;
}

public struct FaceDataRaw
{
    public static readonly Dictionary<Faces, List<Vector3>> rawVertexData =
        new()
        {
            {
                Faces.FRONT,
                new List<Vector3>()
                {
                    new(-0.5f, 0.5f, 0.5f), // topleft vert
                    new(0.5f, 0.5f, 0.5f), // topright vert
                    new(0.5f, -0.5f, 0.5f), // bottomright vert
                    new(-0.5f, -0.5f, 0.5f), // bottomleft vert
                }
            },
            {
                Faces.BACK,
                new List<Vector3>()
                {
                    new(0.5f, 0.5f, -0.5f), // topleft vert
                    new(-0.5f, 0.5f, -0.5f), // topright vert
                    new(-0.5f, -0.5f, -0.5f), // bottomright vert
                    new(0.5f, -0.5f, -0.5f), // bottomleft vert
                }
            },
            {
                Faces.LEFT,
                new List<Vector3>()
                {
                    new(-0.5f, 0.5f, -0.5f), // topleft vert
                    new(-0.5f, 0.5f, 0.5f), // topright vert
                    new(-0.5f, -0.5f, 0.5f), // bottomright vert
                    new(-0.5f, -0.5f, -0.5f), // bottomleft vert
                }
            },
            {
                Faces.RIGHT,
                new List<Vector3>()
                {
                    new(0.5f, 0.5f, 0.5f), // topleft vert
                    new(0.5f, 0.5f, -0.5f), // topright vert
                    new(0.5f, -0.5f, -0.5f), // bottomright vert
                    new(0.5f, -0.5f, 0.5f), // bottomleft vert
                }
            },
            {
                Faces.TOP,
                new List<Vector3>()
                {
                    new(-0.5f, 0.5f, -0.5f), // topleft vert
                    new(0.5f, 0.5f, -0.5f), // topright vert
                    new(0.5f, 0.5f, 0.5f), // bottomright vert
                    new(-0.5f, 0.5f, 0.5f), // bottomleft vert
                }
            },
            {
                Faces.BOTTOM,
                new List<Vector3>()
                {
                    new(-0.5f, -0.5f, 0.5f), // topleft vert
                    new(0.5f, -0.5f, 0.5f), // topright vert
                    new(0.5f, -0.5f, -0.5f), // bottomright vert
                    new(-0.5f, -0.5f, -0.5f), // bottomleft vert
                }
            },
        };
}
