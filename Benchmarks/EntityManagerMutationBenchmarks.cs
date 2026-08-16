using BenchmarkDotNet.Attributes;
using VoxelGame.EntityComponentSystem;

namespace Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class EntityManagerMutationBenchmarks
{
    private EntityManager _entityManager = null!;

    private class TestComponentA : Component { public int Value; }
    private class TestComponentB : Component { public float Data; }

    [Params(512)]
    public int EntityCount;

    [GlobalSetup]
    public void Setup() => _entityManager = new EntityManager();

    [GlobalCleanup]
    public void Cleanup() => _entityManager.Dispose();

    [Benchmark]
    public void AddComponent()
    {
        for (int i = 0; i < EntityCount; i++)
        {
            int entity = _entityManager.CreateEntity();
            _entityManager.AddComponent(entity, new TestComponentA { Value = i });
        }
    }

    [Benchmark]
    public void AddMultipleComponents()
    {
        for (int i = 0; i < EntityCount; i++)
        {
            int entity = _entityManager.CreateEntity();
            _entityManager.AddComponent(entity, new TestComponentA { Value = i });
            _entityManager.AddComponent(entity, new TestComponentB { Data = i });
        }
    }

    [Benchmark]
    public void RemoveEntity()
    {
        int[] entities = new int[EntityCount];
        for (int i = 0; i < EntityCount; i++)
        {
            entities[i] = _entityManager.CreateEntity();
            _entityManager.AddComponent(entities[i], new TestComponentA { Value = i });
        }

        for (int i = 0; i < EntityCount; i++)
        {
            _entityManager.RemoveEntity(entities[i]);
        }
    }

    [Benchmark]
    public void GetComponent()
    {
        int[] entities = new int[EntityCount];
        for (int i = 0; i < EntityCount; i++)
        {
            entities[i] = _entityManager.CreateEntity();
            _entityManager.AddComponent(entities[i], new TestComponentA { Value = i });
        }

        for (int i = 0; i < EntityCount; i++)
        {
            _ = _entityManager.GetComponent<TestComponentA>(entities[i]);
        }
    }
}
