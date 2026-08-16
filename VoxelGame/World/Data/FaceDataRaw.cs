using OpenTK.Mathematics;

namespace VoxelGame.World.Data;

public readonly struct FaceDataRaw
{
    public static readonly Vector3[][] rawVertexData =
    [
        [
            new(-0.5f, 0.5f, 0.5f),
            new(0.5f, 0.5f, 0.5f),
            new(0.5f, -0.5f, 0.5f),
            new(-0.5f, -0.5f, 0.5f),
        ],
        [
            new(0.5f, 0.5f, -0.5f),
            new(-0.5f, 0.5f, -0.5f),
            new(-0.5f, -0.5f, -0.5f),
            new(0.5f, -0.5f, -0.5f),
        ],
        [
            new(-0.5f, 0.5f, -0.5f),
            new(-0.5f, 0.5f, 0.5f),
            new(-0.5f, -0.5f, 0.5f),
            new(-0.5f, -0.5f, -0.5f),
        ],
        [
            new(0.5f, 0.5f, 0.5f),
            new(0.5f, 0.5f, -0.5f),
            new(0.5f, -0.5f, -0.5f),
            new(0.5f, -0.5f, 0.5f),
        ],
        [
            new(-0.5f, 0.5f, -0.5f),
            new(0.5f, 0.5f, -0.5f),
            new(0.5f, 0.5f, 0.5f),
            new(-0.5f, 0.5f, 0.5f),
        ],
        [
            new(-0.5f, -0.5f, 0.5f),
            new(0.5f, -0.5f, 0.5f),
            new(0.5f, -0.5f, -0.5f),
            new(-0.5f, -0.5f, -0.5f),
        ],
    ];

    public static readonly Vector3[] faceNormals =
    [
        new(0, 0, 1),
        new(0, 0, -1),
        new(-1, 0, 0),
        new(1, 0, 0),
        new(0, 1, 0),
        new(0, -1, 0),
    ];
}
