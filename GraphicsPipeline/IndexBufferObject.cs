using OpenTK.Graphics.OpenGL4;

namespace VoxelGame.GraphicsPipeline
{
    public class IndexBufferObject
    {
        public int ID;

        public IndexBufferObject(List<uint> data)
        {
            ID = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ID);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                data.Count * sizeof(uint),
                data.ToArray(),
                BufferUsageHint.StaticDraw
            );
        }

        public void Bind()
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ID);
        }

        public static void Unbind()
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        public void Dispose()
        {
            GL.DeleteBuffer(ID);
        }
    }
}
