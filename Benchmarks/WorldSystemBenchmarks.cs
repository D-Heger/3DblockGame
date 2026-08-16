using BenchmarkDotNet.Attributes;
using VoxelGame.EntityComponentSystem.Systems;
using VoxelGame.World;

namespace Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class WorldSystemBenchmarks
{
    private WorldSystem _worldSystem = null!;
    private ChunkPosition[] _positions = null!;

    [Params(256)]
    public int ChunkCount;

    [GlobalSetup]
    public void Setup()
    {
        _worldSystem = new WorldSystem();
        _positions = new ChunkPosition[ChunkCount];
        for (int i = 0; i < ChunkCount; i++)
        {
            _positions[i] = new ChunkPosition(i, 0);
            _worldSystem.AddChunk(_positions[i], new ChunkData());
        }
    }

    [Benchmark]
    public bool ChunkExists()
    {
        bool result = false;
        for (int i = 0; i < ChunkCount; i++)
        {
            result |= _worldSystem.ChunkExists(_positions[i]);
        }
        return result;
    }

    [Benchmark]
    public ChunkData? GetChunk()
    {
        ChunkData? last = null;
        for (int i = 0; i < ChunkCount; i++)
        {
            last = _worldSystem.GetChunk(_positions[i]);
        }
        return last;
    }
}
