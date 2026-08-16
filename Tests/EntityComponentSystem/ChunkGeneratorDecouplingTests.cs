using VoxelGame.World;
using VoxelGame.World.Data;

namespace Tests.EntityComponentSystem;

/// <summary>
/// Verifies ChunkGenerator is decoupled from ECS: it depends only on the narrow
/// <see cref="IChunkSource"/> read interface, not on the ECS WorldSystem. This
/// keeps generation testable and consumable without an entity manager.
/// </summary>
public class ChunkGeneratorDecouplingTests
{
    /// <summary>
    /// A chunk source with no registered chunks, proving the generator needs no
    /// ECS WorldSystem to mesh a chunk.
    /// </summary>
    private sealed class EmptyChunkSource : IChunkSource
    {
        public ChunkData? GetChunk(ChunkPosition chunkPosition) => null;
    }

    /// <summary>
    /// A dictionary-backed chunk source used to control neighbor lookups.
    /// </summary>
    private sealed class StubChunkSource : IChunkSource
    {
        private readonly Dictionary<ChunkPosition, ChunkData> _chunks = [];

        public void Add(ChunkPosition position, ChunkData data) => _chunks[position] = data;

        public ChunkData? GetChunk(ChunkPosition chunkPosition) =>
            _chunks.TryGetValue(chunkPosition, out ChunkData? data) ? data : null;
    }

    [Fact]
    public void GenerateChunkMesh_WorksWithoutWorldSystem()
    {
        ChunkPosition position = new(0, 0);

        ChunkMeshData mesh = ChunkGenerator.GenerateChunkMesh(
            position,
            new EmptyChunkSource(),
            out ChunkData chunkData
        );

        Assert.NotNull(chunkData);
        Assert.False(mesh.IsEmpty);
        Assert.True(mesh.Vertices.Length > 0);
        Assert.True(mesh.Uses16BitIndices);
    }

    [Fact]
    public void GenerateChunkMesh_CullsBorderFacesUsingChunkSourceNeighbors()
    {
        ChunkPosition selfPosition = new(0, 0);
        ChunkPosition neighborPosition = new(1, 0); // +X neighbor

        ChunkData solidChunk = new();
        solidChunk.Fill(BlockType.DIRT);

        // No neighbor registered: the chunk's +X border faces stay exposed.
        ChunkMeshData exposed = ChunkGenerator.GenerateChunkMesh(
            selfPosition,
            new EmptyChunkSource(),
            solidChunk
        );

        // Solid +X neighbor registered: the border faces facing it are culled.
        StubChunkSource withNeighbor = new();
        ChunkData solidNeighbor = new();
        solidNeighbor.Fill(BlockType.DIRT);
        withNeighbor.Add(neighborPosition, solidNeighbor);
        ChunkMeshData culled = ChunkGenerator.GenerateChunkMesh(selfPosition, withNeighbor, solidChunk);

        Assert.True(
            culled.Vertices.Length < exposed.Vertices.Length,
            $"culled ({culled.Vertices.Length}) should have fewer vertices than exposed ({exposed.Vertices.Length})"
        );
    }
}
