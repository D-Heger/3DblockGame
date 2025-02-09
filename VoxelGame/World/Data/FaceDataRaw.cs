using OpenTK.Mathematics;

namespace VoxelGame.World.Data
{
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

        public static readonly Dictionary<Faces, Vector3> faceNormals = new()
        {
            { Faces.FRONT, new Vector3(0, 0, 1) },
            { Faces.BACK, new Vector3(0, 0, -1) },
            { Faces.LEFT, new Vector3(-1, 0, 0) },
            { Faces.RIGHT, new Vector3(1, 0, 0) },
            { Faces.TOP, new Vector3(0, 1, 0) },
            { Faces.BOTTOM, new Vector3(0, -1, 0) }
        };
    }
}
