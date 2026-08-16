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

    public void LinkToVAO(int location, int size, VertexBufferObject vbo)
    {
        Bind();
        vbo.Bind();
        GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, 0, 0);
        GL.EnableVertexAttribArray(location);
        Unbind();
    }

    public void Bind() => GL.BindVertexArray(ID);

    public static void Unbind() => GL.BindVertexArray(0);

    public void Dispose() => GL.DeleteVertexArray(ID);
}
