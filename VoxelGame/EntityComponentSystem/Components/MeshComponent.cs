using OpenTK.Graphics.OpenGL4;
using VoxelGame.GraphicsPipeline;
using VoxelGame.World.Data;

namespace VoxelGame.EntityComponentSystem.Components;

public class MeshComponent(ChunkMeshData meshData) : Component
{
    public ChunkMeshData MeshData = meshData;

    public VertexArrayObject? VAO;
    public VertexBufferObject<ChunkVertex>? VBO;
    public IndexBufferObject? IBO;

    public bool IsDisposed { get; private set; }

    public int IndexCount => MeshData.IndexCount;

    public bool Uses16BitIndices => MeshData.Uses16BitIndices;

    private bool _buffersInitialized;

    public void SetupBuffers()
    {
        if (_buffersInitialized || MeshData.IsEmpty)
        {
            return;
        }

        VAO = new VertexArrayObject();
        VAO.Bind();

        VBO = new VertexBufferObject<ChunkVertex>(MeshData.Vertices);
        VBO.Bind();

        const int stride = ChunkVertex.SizeInBytes;
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 12);
        GL.EnableVertexAttribArray(1);

        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Byte, true, stride, 20);
        GL.EnableVertexAttribArray(2);

        IBO = MeshData.Indices16 != null
            ? new IndexBufferObject<ushort>(MeshData.Indices16)
            : new IndexBufferObject<uint>(MeshData.Indices32!);
        IBO.Bind();

        VertexArrayObject.Unbind();

        _buffersInitialized = true;
    }

    /// <summary>
    /// Resets the GPU buffers, forcing them to be recreated with new data on next render
    /// </summary>
    public void ResetBuffers()
    {
        if (_buffersInitialized)
        {
            VAO?.Dispose();
            VBO?.Dispose();
            IBO?.Dispose();

            VAO = null;
            VBO = null;
            IBO = null;

            _buffersInitialized = false;
        }
    }

    public void Dispose()
    {
        ResetBuffers();
        IsDisposed = true;
    }
}
