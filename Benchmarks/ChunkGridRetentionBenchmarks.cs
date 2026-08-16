using BenchmarkDotNet.Attributes;
using VoxelGame.EntityComponentSystem.Systems;
using VoxelGame.World;
using VoxelGame.World.Data;

namespace Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class ChunkGridRetentionBenchmarks
{
    private const int GridRadius = 2;
    private const int SurroundRadius = 3;

    private WorldSystem _worldSystem = null!;
    private ChunkPosition[] _surroundingPositions = null!;
    private ChunkData[] _surroundingChunks = null!;

    [GlobalSetup]
    public void Setup()
    {
        int side = 2 * SurroundRadius + 1;
        _surroundingPositions = new ChunkPosition[side * side];
        _surroundingChunks = new ChunkData[side * side];

        // Pre-create solid surrounding chunks (explicitly filled with DIRT,
        // since AIR is now the zero default) so the generated grid chunks
        // behave like interior chunks.
        int i = 0;
        for (int x = -SurroundRadius; x <= SurroundRadius; x++)
        {
            for (int z = -SurroundRadius; z <= SurroundRadius; z++)
            {
                _surroundingPositions[i] = new ChunkPosition(x, z);
                ChunkData surround = new();
                surround.Fill(BlockType.DIRT);
                _surroundingChunks[i] = surround;
                i++;
            }
        }

        ResetWorld();
    }

    [IterationSetup]
    public void ResetWorld()
    {
        _worldSystem = new WorldSystem();
        for (int i = 0; i < _surroundingPositions.Length; i++)
        {
            _worldSystem.AddChunk(_surroundingPositions[i], _surroundingChunks[i]);
        }
    }

    [Benchmark]
    public WorldSystem GenerateChunkGrid_5x5()
    {
        for (int x = -GridRadius; x <= GridRadius; x++)
        {
            for (int z = -GridRadius; z <= GridRadius; z++)
            {
                ChunkPosition pos = new(x, z);
                ChunkGenerator.GenerateChunkMesh(pos, _worldSystem, out ChunkData chunkData);
                _worldSystem.AddChunk(pos, chunkData);
            }
        }
        return _worldSystem;
    }
}
