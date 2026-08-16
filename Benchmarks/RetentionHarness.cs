using VoxelGame.EntityComponentSystem.Systems;
using VoxelGame.World;
using VoxelGame.World.Data;

namespace Benchmarks;

/// <summary>
/// Non-BDN harness that measures retained bytes per chunk at grid scale.
/// Workload mirrors ChunkGridRetentionTests exactly: a solid surround RING at
/// radius 3 (24 chunks, the cells with max(|x|,|z|) == 3, so they never
/// overlap the grid) plus a generated grid of radius 2 (25 chunks), meshed in
/// a second pass with all neighbors present (the steady state the game
/// converges to via arrival re-meshing). Run with:
/// dotnet run --project Benchmarks --configuration Release -- retention
/// </summary>
public static class RetentionHarness
{
    private const int GridRadius = 2;
    private const int SurroundRadius = 3;

    public static void Run()
    {
        WorldSystem worldSystem = new();

        for (int x = -SurroundRadius; x <= SurroundRadius; x++)
        {
            for (int z = -SurroundRadius; z <= SurroundRadius; z++)
            {
                if (Math.Max(Math.Abs(x), Math.Abs(z)) != SurroundRadius)
                {
                    continue;
                }
                worldSystem.AddChunk(new ChunkPosition(x, z), CreateSolidSurroundChunk());
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

        // Pass 2: mesh every chunk with all neighbors present so all internal
        // border faces are culled (matches steady-state re-meshed chunks).
        List<ChunkMeshData> meshes = [];
        foreach (ChunkPosition pos in gridPositions)
        {
            meshes.Add(ChunkGenerator.GenerateChunkMesh(pos, worldSystem, out _));
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long after = GC.GetTotalMemory(true);

        GC.KeepAlive(meshes);
        GC.KeepAlive(worldSystem);

        int chunkCount = gridPositions.Count;
        long totalBytes = after - before;
        double perChunkKB = totalBytes / 1024.0 / chunkCount;
        Console.WriteLine($"retention: chunks={chunkCount} total={totalBytes} bytes retained_per_chunk={perChunkKB:F1} KB");
    }

    private static ChunkData CreateSolidSurroundChunk()
    {
        // Surround chunks must be fully solid so generated grid chunks behave
        // like interior chunks. AIR is the zero default, so the surround is
        // filled explicitly (on the baseline the flat array zero-inits to DIRT).
        ChunkData surround = new();
        surround.Fill(BlockType.DIRT);
        return surround;
    }
}
