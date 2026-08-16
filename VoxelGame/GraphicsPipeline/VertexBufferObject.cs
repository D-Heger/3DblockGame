using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace VoxelGame.GraphicsPipeline;

public class VertexBufferObject
{
    public int ID;

    public VertexBufferObject(List<Vector3> data)
    {
        ID = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, ID);
        Span<Vector3> span = CollectionsMarshal.AsSpan(data);
        unsafe
        {
            fixed (void* p = span)
            {
                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    span.Length * Vector3.SizeInBytes,
                    (IntPtr)p,
                    BufferUsageHint.StaticDraw
                );
            }
        }
    }

    public VertexBufferObject(List<Vector2> data)
    {
        ID = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, ID);
        Span<Vector2> span = CollectionsMarshal.AsSpan(data);
        unsafe
        {
            fixed (void* p = span)
            {
                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    span.Length * Vector2.SizeInBytes,
                    (IntPtr)p,
                    BufferUsageHint.StaticDraw
                );
            }
        }
    }

    public void Bind() => GL.BindBuffer(BufferTarget.ArrayBuffer, ID);

    public static void Unbind() => GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

    public void Dispose() => GL.DeleteBuffer(ID);
}
