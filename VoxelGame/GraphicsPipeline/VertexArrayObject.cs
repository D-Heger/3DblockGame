using OpenTK.Graphics.OpenGL4;

namespace VoxelGame.GraphicsPipeline;

public class VertexArrayObject
{
    public int ID;

    public VertexArrayObject()
    {
        ID = GL.GenVertexArray();
        GL.BindVertexArray(ID);
    }

    public void Bind() => GL.BindVertexArray(ID);

    public static void Unbind() => GL.BindVertexArray(0);

    public void Dispose() => GL.DeleteVertexArray(ID);
}
