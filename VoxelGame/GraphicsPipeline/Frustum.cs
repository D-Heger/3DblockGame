using OpenTK.Mathematics;

namespace VoxelGame.GraphicsPipeline;

public class Frustum
{
    private readonly Plane[] _planes = new Plane[6];

    public void UpdateFrustum(Matrix4 viewProjection)
    {
        // Left plane
        _planes[0] = new Plane(
            viewProjection.M14 + viewProjection.M11,
            viewProjection.M24 + viewProjection.M21,
            viewProjection.M34 + viewProjection.M31,
            viewProjection.M44 + viewProjection.M41
        );

        // Right plane
        _planes[1] = new Plane(
            viewProjection.M14 - viewProjection.M11,
            viewProjection.M24 - viewProjection.M21,
            viewProjection.M34 - viewProjection.M31,
            viewProjection.M44 - viewProjection.M41
        );

        // Bottom plane
        _planes[2] = new Plane(
            viewProjection.M14 + viewProjection.M12,
            viewProjection.M24 + viewProjection.M22,
            viewProjection.M34 + viewProjection.M32,
            viewProjection.M44 + viewProjection.M42
        );

        // Top plane
        _planes[3] = new Plane(
            viewProjection.M14 - viewProjection.M12,
            viewProjection.M24 - viewProjection.M22,
            viewProjection.M34 - viewProjection.M32,
            viewProjection.M44 - viewProjection.M42
        );

        // Near plane
        _planes[4] = new Plane(
            viewProjection.M14 + viewProjection.M13,
            viewProjection.M24 + viewProjection.M23,
            viewProjection.M34 + viewProjection.M33,
            viewProjection.M44 + viewProjection.M43
        );

        // Far plane
        _planes[5] = new Plane(
            viewProjection.M14 - viewProjection.M13,
            viewProjection.M24 - viewProjection.M23,
            viewProjection.M34 - viewProjection.M33,
            viewProjection.M44 - viewProjection.M43
        );

        // Normalize planes
        for (int i = 0; i < 6; i++)
        {
            _planes[i].Normalize();
        }
    }

    public bool IsBoxInsideFrustum(Vector3 min, Vector3 max)
    {
        foreach (Plane plane in _planes)
        {
            Vector3 positiveVertex = new(
                plane.Normal.X >= 0 ? max.X : min.X,
                plane.Normal.Y >= 0 ? max.Y : min.Y,
                plane.Normal.Z >= 0 ? max.Z : min.Z
            );

            if (plane.GetDistanceToPoint(positiveVertex) < 0)
            {
                return false;
            }
        }
        return true;
    }
}

public struct Plane(float a, float b, float c, float d)
{
    public Vector3 Normal = new(a, b, c);
    public float D = d;

    public void Normalize()
    {
        float length = Normal.Length;
        Normal /= length;
        D /= length;
    }

    public readonly float GetDistanceToPoint(Vector3 point) => Vector3.Dot(Normal, point) + D;
}
