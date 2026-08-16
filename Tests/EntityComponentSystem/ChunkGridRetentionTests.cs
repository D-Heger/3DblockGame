using VoxelGame.EntityComponentSystem.Systems;
using VoxelGame.World;
using VoxelGame.World.Data;

namespace Tests.EntityComponentSystem;

[CollectionDefinition("MemorySensitive", DisableParallelization = true)]
public class MemorySensitiveCollection { }

/// <summary>
/// Guard against per-chunk retained-memory regressions at chunk-grid scale.
/// The per-chunk benchmark only measures single-chunk allocations and is blind
/// to retained List capacity accumulated across thousands of live chunks.
/// </summary>
[Collection("MemorySensitive")]
public class ChunkGridRetentionTests
{
    private const int GridRadius = 2;
    private const int SurroundRadius = 3;
    private const long PerChunkBudgetBytes = 180 * 1024;

    [Fact]
    public void GenerateChunkGrid_RetainedMemory_StaysWithinPerChunkBudget()
    {
        int size = Chunk.SIZE;
        WorldSystem worldSystem = new();

        // Surround the grid with solid chunks (default block = DIRT) so faces
        // facing outward are culled, mimicking interior chunks at real scale.
        for (int x = -SurroundRadius; x <= SurroundRadius; x++)
        {
            for (int z = -SurroundRadius; z <= SurroundRadius; z++)
            {
                ChunkPosition pos = new(x * size, 0, z * size);
                worldSystem.AddChunk(pos, new ChunkData(size, Chunk.HEIGHT, size));
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long before = GC.GetTotalMemory(true);

        List<ChunkMeshData> meshes = [];
        int chunkCount = 0;
        for (int x = -GridRadius; x <= GridRadius; x++)
        {
            for (int z = -GridRadius; z <= GridRadius; z++)
            {
                ChunkPosition pos = new(x * size, 0, z * size);
                ChunkMeshData mesh = ChunkGenerator.GenerateChunkMesh(pos, worldSystem, out ChunkData? chunkData);
                worldSystem.AddChunk(pos, chunkData);
                meshes.Add(mesh);
                chunkCount++;
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long after = GC.GetTotalMemory(true);

        Assert.Equal(25, chunkCount);
        Assert.All(meshes, mesh => Assert.True(mesh.Vertices.Count > 0));

        long perChunkBytes = (after - before) / chunkCount;
        Assert.True(
            perChunkBytes <= PerChunkBudgetBytes,
            $"Per-chunk retained memory {perChunkBytes / 1024.0:F1} KB exceeds budget of "
                + $"{PerChunkBudgetBytes / 1024} KB"
        );
    }
}
