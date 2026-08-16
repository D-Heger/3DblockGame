using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace VoxelGame.World.Data;

/// <summary>
/// Interleaved chunk vertex: float3 position + float2 UV + byte4 SNORM normal
/// packed into a uint. Fixed 24 bytes per vertex.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct ChunkVertex(Vector3 position, Vector2 uv, uint packedNormal)
{
    public const int SizeInBytes = 24;

    public Vector3 Position = position;
    public Vector2 UV = uv;
    public uint Normal = packedNormal;

    /// <summary>
    /// Packs an axis-aligned unit normal into 3 signed bytes (±127), laid out
    /// as X, Y, Z in the low three bytes for SNORM unpacking in the shader.
    /// </summary>
    public static uint PackNormal(in Vector3 normal)
        => (uint)(byte)(sbyte)(normal.X * sbyte.MaxValue)
            | ((uint)(byte)(sbyte)(normal.Y * sbyte.MaxValue) << 8)
            | ((uint)(byte)(sbyte)(normal.Z * sbyte.MaxValue) << 16);

    public readonly Vector3 UnpackNormal()
    {
        float x = (sbyte)(byte)Normal / (float)sbyte.MaxValue;
        float y = (sbyte)(byte)(Normal >> 8) / (float)sbyte.MaxValue;
        float z = (sbyte)(byte)(Normal >> 16) / (float)sbyte.MaxValue;
        return Vector3.Normalize(new Vector3(x, y, z));
    }
}
