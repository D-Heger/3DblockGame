using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using VoxelGame.GraphicsPipeline;

namespace VoxelGame.EntityComponentSystem.Components;

public class MeshComponent(
    List<Vector3> vertices,
    List<Vector2> uvs,
    List<Vector3> normals,
    List<uint> indices
) : Component
{
    public List<Vector3> Vertices = vertices;
    public List<Vector2> UVs = uvs;
    public List<Vector3> Normals = normals;
    public List<uint> Indices = indices;

    public VertexArrayObject? VAO;
    public VertexBufferObject? VBO;
    public VertexBufferObject? UVBO;
    public VertexBufferObject? NormalBO;
    public IndexBufferObject? IBO;

    private bool buffersInitialized;

    public void SetupBuffers()
    {
        if (buffersInitialized)
        {
            return;
        }

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

        NormalBO = new VertexBufferObject(Normals);
        NormalBO.Bind();
        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 0, 0);
        GL.EnableVertexAttribArray(2);

        IBO = new IndexBufferObject(Indices);
        IBO.Bind();

        VertexArrayObject.Unbind();

        buffersInitialized = true;
    }

    /// <summary>
    /// Resets the GPU buffers, forcing them to be recreated with new data on next render
    /// </summary>
    public void ResetBuffers()
    {
        if (buffersInitialized)
        {
            // Dispose existing buffers
            VAO?.Dispose();
            VBO?.Dispose();
            UVBO?.Dispose();
            NormalBO?.Dispose();
            IBO?.Dispose();

            // Reset buffer objects
            VAO = null;
            VBO = null;
            UVBO = null;
            NormalBO = null;
            IBO = null;

            buffersInitialized = false;
        }
    }

    public void Dispose()
    {
        if (!buffersInitialized)
        {
            return;
        }

        VAO?.Dispose();
        VBO?.Dispose();
        UVBO?.Dispose();
        NormalBO?.Dispose();
        IBO?.Dispose();
        buffersInitialized = false;
    }
}
