using OpenTK.Graphics.OpenGL4;

namespace VoxelGame.GraphicsPipeline;

public class VertexBufferObject<T> where T : unmanaged
{
    public int ID;

    public VertexBufferObject(ReadOnlySpan<T> data)
    {
        ID = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, ID);
        unsafe
        {
            fixed (T* p = data)
            {
                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    data.Length * sizeof(T),
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
