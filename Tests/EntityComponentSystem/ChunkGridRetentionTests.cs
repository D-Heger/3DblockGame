using VoxelGame.EntityComponentSystem.Systems;
using VoxelGame.World;
using VoxelGame.World.Data;

namespace Tests.EntityComponentSystem;

[CollectionDefinition("MemorySensitive", DisableParallelization = true)]
public class MemorySensitiveCollection { }

/// <summary>
/// Guard against per-chunk retained-memory regressions at chunk-grid scale.
/// The per-chunk benchmark only measures single-chunk allocations and is blind
/// to retained capacity accumulated across thousands of live chunks. Measures
/// steady-state retention (all neighbors present during meshing, as after
/// arrival re-meshing in game): chunk block data + final mesh arrays.
/// </summary>
[Collection("MemorySensitive")]
public class ChunkGridRetentionTests
{
    private const int GridRadius = 2;
    private const int SurroundRadius = 3;
    private const long PerChunkBudgetBytes = 96 * 1024;

    [Fact]
    public void GenerateChunkGrid_RetainedMemory_StaysWithinPerChunkBudget()
    {
        WorldSystem worldSystem = new();

        // Surround the grid with a solid RING of chunks (cells at radius 3,
        // never overlapping the grid) so faces facing outward are culled,
        // mimicking interior chunks at real scale. (Default block is AIR now,
        // so the surround must be filled explicitly.)
        for (int x = -SurroundRadius; x <= SurroundRadius; x++)
        {
            for (int z = -SurroundRadius; z <= SurroundRadius; z++)
            {
                if (Math.Max(Math.Abs(x), Math.Abs(z)) != SurroundRadius)
                {
                    continue;
                }
                ChunkData surround = new();
                surround.Fill(BlockType.DIRT);
                worldSystem.AddChunk(new ChunkPosition(x, z), surround);
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long before = GC.GetTotalMemory(true);

        // Pass 1: generate + register all block data first.
        List<ChunkPosition> gridPositions = [];
        for (int x = -GridRadius; x <= GridRadius; x++)
        {
            for (int z = -GridRadius; z <= GridRadius; z++)
            {
                ChunkPosition pos = new(x, z);
                worldSystem.AddChunk(pos, ChunkGenerator.GenerateChunkData(pos));
                gridPositions.Add(pos);
            }
        }

        // Pass 2: mesh every chunk with all neighbors present. This models the
        // steady state the game converges to via arrival re-meshing, where all
        // internal border faces are culled (initial single-pass generation keeps
        // extra transient faces until neighbors arrive).
        List<ChunkMeshData> meshes = [];
        foreach (ChunkPosition pos in gridPositions)
        {
            meshes.Add(ChunkGenerator.GenerateChunkMesh(pos, worldSystem, out _));
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long after = GC.GetTotalMemory(true);

        int chunkCount = gridPositions.Count;
        Assert.Equal(25, chunkCount);
        Assert.All(meshes, mesh => Assert.False(mesh.IsEmpty));

        long perChunkBytes = (after - before) / chunkCount;
        Assert.True(
            perChunkBytes <= PerChunkBudgetBytes,
            $"Per-chunk retained memory {perChunkBytes / 1024.0:F1} KB exceeds budget of "
                + $"{PerChunkBudgetBytes / 1024} KB"
        );
    }
}
