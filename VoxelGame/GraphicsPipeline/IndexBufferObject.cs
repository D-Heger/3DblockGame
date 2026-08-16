using OpenTK.Graphics.OpenGL4;

namespace VoxelGame.GraphicsPipeline;

public class IndexBufferObject
{
    public int ID;

    protected IndexBufferObject()
    {
        ID = GL.GenBuffer();
    }

    public void Bind() => GL.BindBuffer(BufferTarget.ElementArrayBuffer, ID);

    public static void Unbind() => GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

    public void Dispose() => GL.DeleteBuffer(ID);
}

public sealed class IndexBufferObject<T> : IndexBufferObject where T : unmanaged
{
    public IndexBufferObject(ReadOnlySpan<T> data)
    {
        Bind();
        unsafe
        {
            fixed (T* p = data)
            {
                GL.BufferData(
                    BufferTarget.ElementArrayBuffer,
                    data.Length * sizeof(T),
                    (IntPtr)p,
                    BufferUsageHint.StaticDraw
                );
            }
        }
    }
}
