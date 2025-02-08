using VoxelGame.EntityComponentSystem;
using OpenTK.Mathematics;

namespace Tests;

public class EntityManagerTests : IDisposable
{
    private readonly EntityManager _entityManager;

    public class TestComponent : Component
    {
        public int Value { get; set; }
    }

    public class SecondTestComponent : Component
    {
        public string Text { get; set; }
    }

    public EntityManagerTests()
    {
        _entityManager = new EntityManager();
    }

    public void Dispose()
    {
        _entityManager.Dispose();
    }

    [Fact]
    public void CreateEntity_AssignsUniqueIds()
    {
        int entity1 = _entityManager.CreateEntity();
        int entity2 = _entityManager.CreateEntity();

        Assert.NotEqual(entity1, entity2);
    }

    [Fact]
    public void AddAndGetComponent_WorksCorrectly()
    {
        int entity = _entityManager.CreateEntity();
        var component = new TestComponent { Value = 42 };
        
        _entityManager.AddComponent(entity, component);
        var retrievedComponent = _entityManager.GetComponent<TestComponent>(entity);
        
        Assert.NotNull(retrievedComponent);
        Assert.Equal(42, retrievedComponent.Value);
    }

    [Fact]
    public void GetEntitiesWithComponent_ReturnsCorrectEntities()
    {
        int entity1 = _entityManager.CreateEntity();
        int entity2 = _entityManager.CreateEntity();
        
        _entityManager.AddComponent(entity1, new TestComponent());
        _entityManager.AddComponent(entity2, new TestComponent());
        _entityManager.AddComponent(entity2, new SecondTestComponent());

        var entitiesWithTest = _entityManager.GetEntitiesWithComponent<TestComponent>().ToList();
        var entitiesWithSecond = _entityManager.GetEntitiesWithComponent<SecondTestComponent>().ToList();

        Assert.Equal(2, entitiesWithTest.Count);
        Assert.Contains(entity1, entitiesWithTest);
        Assert.Contains(entity2, entitiesWithTest);
        Assert.Single(entitiesWithSecond);
        Assert.Contains(entity2, entitiesWithSecond);
    }

    [Fact]
    public void RemoveEntity_RemovesEntityAndComponents()
    {
        int entity = _entityManager.CreateEntity();
        _entityManager.AddComponent(entity, new TestComponent());
        
        _entityManager.RemoveEntity(entity);
        
        Assert.False(_entityManager.EntityExists(entity));
        Assert.Null(_entityManager.GetComponent<TestComponent>(entity));
    }

    [Fact]
    public void GetEntitiesWithMultipleComponents_ReturnsCorrectEntities()
    {
        int entity1 = _entityManager.CreateEntity();
        int entity2 = _entityManager.CreateEntity();
        
        _entityManager.AddComponent(entity1, new TestComponent());
        _entityManager.AddComponent(entity1, new SecondTestComponent());
        _entityManager.AddComponent(entity2, new TestComponent());

        var entitiesWithBoth = _entityManager.GetEntitiesWithComponents<TestComponent, SecondTestComponent>().ToList();

        Assert.Single(entitiesWithBoth);
        Assert.Contains(entity1, entitiesWithBoth);
    }
}