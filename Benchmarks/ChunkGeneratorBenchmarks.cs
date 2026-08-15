using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using VoxelGame.EntityComponentSystem.Systems;
using VoxelGame.World;
using VoxelGame.World.Data;

namespace Benchmarks;

[MemoryDiagnoser]
public class ChunkGeneratorBenchmarks
{
    private WorldSystem _worldSystem = null!;

    [GlobalSetup]
    public void Setup()
    {
        _worldSystem = new WorldSystem();
        var center = new ChunkPosition(0, 0, 0);
        var blocks = new BlockType[Chunk.SIZE, Chunk.HEIGHT, Chunk.SIZE];
        _worldSystem.AddChunk(center, new ChunkData(blocks));
    }

    [Benchmark]
    public ChunkMeshData GenerateChunkMesh()
    {
        var pos = new ChunkPosition(0, 0, 0);
        return ChunkGenerator.GenerateChunkMesh(pos, _worldSystem, out _);
    }
}
