namespace VoxelGame.World.Data;

public class ChunkMeshData
{
    // Vertices at or below this count use 16-bit indices; above it the whole
    // chunk falls back to 32-bit indices (no sub-mesh splitting).
    public const int Max16BitVertices = 65532;

    public ChunkVertex[] Vertices = [];
    public ushort[]? Indices16;
    public uint[]? Indices32;

    public int IndexCount => Indices16?.Length ?? Indices32?.Length ?? 0;

    public bool Uses16BitIndices => Indices16 != null;

    public bool IsEmpty => IndexCount == 0;
}
