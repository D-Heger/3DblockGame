using OpenTK.Mathematics;

namespace VoxelGame.World.Data
{
    public class ChunkMeshData
    {
        public List<Vector3> Vertices;
        public List<Vector2> UVs;
        public List<uint> Indices;

        public ChunkMeshData()
        {
            Vertices = [];
            UVs = [];
            Indices = [];
        }
    }
}
