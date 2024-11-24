using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

public class MeshComponent : Component
{
    public List<Vector3> Vertices;
    public List<Vector2> UVs;
    public List<uint> Indices;

    public VertexArrayObject VAO;
    public VertexBufferObject VBO;
    public VertexBufferObject UVBO;
    public IndexBufferObject IBO;

    private bool buffersInitialized = false;

    public MeshComponent(List<Vector3> vertices, List<Vector2> uvs, List<uint> indices)
    {
        Vertices = vertices;
        UVs = uvs;
        Indices = indices;
    }

    public void SetupBuffers()
    {
        if (buffersInitialized)
            return;

        VAO = new VertexArrayObject();
        VAO.Bind();

        VBO = new VertexBufferObject(Vertices);
        VBO.Bind();
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
        GL.EnableVertexAttribArray(0);

        UVBO = new VertexBufferObject(UVs);
        UVBO.Bind();
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);
        GL.EnableVertexAttribArray(1);

        IBO = new IndexBufferObject(Indices);
        IBO.Bind();

        VAO.Unbind();

        buffersInitialized = true;
    }

    public void Dispose()
    {
        if (buffersInitialized)
        {
            VAO.Dispose();
            VBO.Dispose();
            UVBO.Dispose();
            IBO.Dispose();
            buffersInitialized = false;
        }
    }
}