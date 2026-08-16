using BenchmarkDotNet.Attributes;
using VoxelGame.EntityComponentSystem.Systems;
using VoxelGame.World;
using VoxelGame.World.Data;

namespace Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class ChunkGeneratorBenchmarks
{
    private WorldSystem _worldSystem = null!;

    [GlobalSetup]
    public void Setup()
    {
        _worldSystem = new WorldSystem();
        ChunkPosition center = new(0, 0);
        _worldSystem.AddChunk(center, new ChunkData());
    }

    [Benchmark]
    public ChunkMeshData GenerateChunkMesh()
    {
        ChunkPosition pos = new(0, 0);
        return ChunkGenerator.GenerateChunkMesh(pos, _worldSystem, out _);
    }
}
