using BenchmarkDotNet.Attributes;
using VoxelGame.EntityComponentSystem;
using VoxelGame.EntityComponentSystem.Components;
using OpenTK.Mathematics;

namespace Benchmarks;

[MemoryDiagnoser]
public class EntityManagerBenchmarks
{
    private EntityManager _entityManager = null!;

    private class BenchComponentA : Component { public int Value; }
    private class BenchComponentB : Component { public float Data; }
    private class BenchComponentC : Component { public string? Text; }

    [Params(100, 1000, 10000)]
    public int EntityCount;

    [GlobalSetup]
    public void Setup()
    {
        _entityManager = new EntityManager();
        for (int i = 0; i < EntityCount; i++)
        {
            int entity = _entityManager.CreateEntity();
            _entityManager.AddComponent(entity, new BenchComponentA { Value = i });
            if (i % 2 == 0)
                _entityManager.AddComponent(entity, new BenchComponentB { Data = i });
            if (i % 3 == 0)
                _entityManager.AddComponent(entity, new BenchComponentC { Text = $"entity_{i}" });
        }
    }

    [GlobalCleanup]
    public void Cleanup() => _entityManager.Dispose();

    [Benchmark]
    public int GetEntitiesWithComponent_Single()
    {
        int count = 0;
        foreach (var _ in _entityManager.GetEntitiesWithComponent<BenchComponentA>())
            count++;
        return count;
    }

    [Benchmark]
    public int GetEntitiesWithComponents_Two()
    {
        int count = 0;
        foreach (var _ in _entityManager.GetEntitiesWithComponents<BenchComponentA, BenchComponentB>())
            count++;
        return count;
    }

    [Benchmark]
    public int GetEntitiesWithComponents_Three()
    {
        int count = 0;
        foreach (var _ in _entityManager.GetEntitiesWithComponents<BenchComponentA, BenchComponentB, BenchComponentC>())
            count++;
        return count;
    }
}
