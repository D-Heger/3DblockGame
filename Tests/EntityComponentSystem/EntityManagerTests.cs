using System.Runtime.CompilerServices;
using VoxelGame.EntityComponentSystem;

namespace Tests.EntityComponentSystem;

/// <summary>
/// Test suite for the EntityManager class. Verifies entity creation, component management,
/// and entity lifecycle functionality in the entity component system.
/// </summary>
[Collection("MemorySensitive")]
public class EntityManagerTests : IDisposable
{
    private readonly EntityManager _entityManager;

    /// <summary>
    /// Test component class used for verifying component management functionality.
    /// Contains a simple integer value for testing purposes.
    /// </summary>
    public class TestComponent : Component
    {
        public int Value { get; set; }
    }

    /// <summary>
    /// Secondary test component class used for testing multiple component interactions.
    /// Contains a simple string property for testing purposes.
    /// </summary>
    public class SecondTestComponent : Component
    {
        public string? Text { get; set; }
    }

    /// <summary>
    /// Initializes a new instance of the EntityManagerTests class.
    /// Sets up a fresh EntityManager for each test.
    /// </summary>
    public EntityManagerTests()
    {
        _entityManager = new EntityManager();
    }

    /// <summary>
    /// Cleans up resources used by the test class.
    /// </summary>
    public void Dispose()
    {
        _entityManager.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Tests that CreateEntity assigns unique IDs to each entity.
    /// </summary>
    [Fact]
    public void CreateEntity_AssignsUniqueIds()
    {
        int entity1 = _entityManager.CreateEntity();
        int entity2 = _entityManager.CreateEntity();

        Assert.NotEqual(entity1, entity2);
    }

    /// <summary>
    /// Tests that adding and retrieving a component works correctly.
    /// </summary>
    [Fact]
    public void AddAndGetComponent_WorksCorrectly()
    {
        int entity = _entityManager.CreateEntity();
        TestComponent component = new() { Value = 42 };

        _entityManager.AddComponent(entity, component);
        TestComponent? retrievedComponent = _entityManager.GetComponent<TestComponent>(entity);

        Assert.NotNull(retrievedComponent);
        Assert.Equal(42, retrievedComponent.Value);
    }

    /// <summary>
    /// Tests that GetEntitiesWithComponent returns the correct entities.
    /// </summary>
    [Fact]
    public void GetEntitiesWithComponent_ReturnsCorrectEntities()
    {
        int entity1 = _entityManager.CreateEntity();
        int entity2 = _entityManager.CreateEntity();

        _entityManager.AddComponent(entity1, new TestComponent());
        _entityManager.AddComponent(entity2, new TestComponent());
        _entityManager.AddComponent(entity2, new SecondTestComponent());

        List<int> entitiesWithTest = [.. _entityManager.GetEntitiesWithComponent<TestComponent>()];
        List<int> entitiesWithSecond = [.. _entityManager.GetEntitiesWithComponent<SecondTestComponent>()];

        Assert.Equal(2, entitiesWithTest.Count);
        Assert.Contains(entity1, entitiesWithTest);
        Assert.Contains(entity2, entitiesWithTest);
        Assert.Single(entitiesWithSecond);
        Assert.Contains(entity2, entitiesWithSecond);
    }

    /// <summary>
    /// Tests that removing an entity also removes its components.
    /// </summary>
    [Fact]
    public void RemoveEntity_RemovesEntityAndComponents()
    {
        int entity = _entityManager.CreateEntity();
        _entityManager.AddComponent(entity, new TestComponent());

        _entityManager.RemoveEntity(entity);

        Assert.False(_entityManager.EntityExists(entity));
        Assert.Null(_entityManager.GetComponent<TestComponent>(entity));
    }

    /// <summary>
    /// Regression test: removed components must become collectable. A previous
    /// component pool retained every removed component forever, pinning each
    /// unloaded chunk's mesh arrays (~100 KB) and growing process memory
    /// unboundedly while the player walked.
    /// </summary>
    [Fact]
    public void RemovedComponents_AreNotRetainedForGarbageCollection()
    {
        WeakReference componentRef = TrackRemovedComponent(_entityManager);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(componentRef.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference TrackRemovedComponent(EntityManager entityManager)
    {
        int entity = entityManager.CreateEntity();
        TestComponent component = new() { Value = 7 };
        entityManager.AddComponent(entity, component);
        WeakReference componentRef = new(component);

        entityManager.RemoveComponent<TestComponent>(entity);
        entityManager.RemoveEntity(entity);

        return componentRef;
    }

    /// <summary>
    /// Tests that GetEntitiesWithMultipleComponents returns the correct entities.
    /// </summary>
    [Fact]
    public void GetEntitiesWithMultipleComponents_ReturnsCorrectEntities()
    {
        int entity1 = _entityManager.CreateEntity();
        int entity2 = _entityManager.CreateEntity();

        _entityManager.AddComponent(entity1, new TestComponent());
        _entityManager.AddComponent(entity1, new SecondTestComponent());
        _entityManager.AddComponent(entity2, new TestComponent());

        List<int> entitiesWithBoth = [.. _entityManager.GetEntitiesWithComponents<TestComponent, SecondTestComponent>()];

        Assert.Single(entitiesWithBoth);
        Assert.Contains(entity1, entitiesWithBoth);
    }
}
