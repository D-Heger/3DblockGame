using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenTK.Mathematics;

namespace VoxelGame.EntityComponentSystem;

public class EntityManager
{
    private int _nextEntityId;
    private readonly Dictionary<int, List<Component>> _entityComponents = [];
    private readonly Dictionary<Type, Dictionary<int, Component>> _componentsByType = [];
    private readonly Dictionary<Type, Stack<Component>> _componentPools = [];
    private readonly Dictionary<int, Vector3> _entityPositions = [];

    // Archetype optimization
    private readonly Dictionary<ArchetypeKey, HashSet<int>> _entityArchetypes = [];
    private readonly Dictionary<int, ArchetypeKey> _entityToArchetype = [];

    // Query result caching
    private static readonly Dictionary<ArchetypeKey, int[]> _queryCache = [];
    private static readonly int[] EmptyIntArray = [];
    private static readonly HashSet<int> EntityQueryCache = [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ArchetypeKey GetArchetypeKey(params Type[] componentTypes)
    {
        Array.Sort(componentTypes, (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return new ArchetypeKey(componentTypes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CreateEntity()
    {
        int entityId = _nextEntityId++;
        _entityComponents[entityId] = new(4);
        _entityToArchetype[entityId] = ArchetypeKey.Empty;
        return entityId;
    }

    public int EntityCount => _entityComponents.Count;

    public void AddComponent<T>(int entityId, T component) where T : Component
    {
        if (!_entityComponents.TryGetValue(entityId, out List<Component>? value))
        {
            throw new Exception("Entity does not exist");
        }

        value.Add(component);

        Type type = typeof(T);
        if (!_componentsByType.TryGetValue(type, out Dictionary<int, Component>? componentDict))
        {
            componentDict = [];
            _componentsByType[type] = componentDict;
        }
        componentDict[entityId] = component;

        // Update archetype
        UpdateEntityArchetype(entityId);

        // Invalidate query cache
        _queryCache.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateEntityArchetype(int entityId)
    {
        if (_entityComponents.TryGetValue(entityId, out List<Component>? components))
        {
            // Remove from old archetype
            ArchetypeKey oldArchetype = _entityToArchetype[entityId];
            if (_entityArchetypes.TryGetValue(oldArchetype, out HashSet<int>? oldSet))
            {
                oldSet.Remove(entityId);
            }

            // Calculate new archetype
            Type[] types = [.. components.Select(c => c.GetType())];
            ArchetypeKey newArchetype = GetArchetypeKey(types);
            _entityToArchetype[entityId] = newArchetype;

            // Add to new archetype
            if (!_entityArchetypes.TryGetValue(newArchetype, out HashSet<int>? entitySet))
            {
                entitySet = [];
                _entityArchetypes[newArchetype] = entitySet;
            }
            entitySet.Add(entityId);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? GetComponent<T>(int entityId) where T : Component
    {
        Type type = typeof(T);
        if (_componentsByType.TryGetValue(type, out Dictionary<int, Component>? components) &&
            components.TryGetValue(entityId, out Component? component))
        {
            return (T)component;
        }
        Debug.WriteLine($"Component {type.Name} not found on entity {entityId}");
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<int> GetEntitiesWithComponent<T>() where T : Component
    {
        Type type = typeof(T);
        ArchetypeKey cacheKey = GetArchetypeKey(type);

        if (_queryCache.TryGetValue(cacheKey, out int[]? cached))
        {
            return cached;
        }

        if (_componentsByType.TryGetValue(type, out Dictionary<int, Component>? components))
        {
            int[] result = [.. components.Keys];
            _queryCache[cacheKey] = result;
            return result;
        }

        return EmptyIntArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEntityWithComponent<T>() where T : Component
    {
        IEnumerable<int> entities = GetEntitiesWithComponent<T>();
        return entities.Any() ? entities.First() : -1;
    }

    public IEnumerable<int> GetEntitiesWithComponents<T1, T2>()
        where T1 : Component
        where T2 : Component
    {
        ArchetypeKey cacheKey = GetArchetypeKey(typeof(T1), typeof(T2));

        if (_queryCache.TryGetValue(cacheKey, out int[]? cached))
        {
            return cached;
        }

        EntityQueryCache.Clear();
        IEnumerable<int> entities1 = GetEntitiesWithComponent<T1>();

        foreach (int entity in entities1)
        {
            if (GetComponent<T2>(entity) != null)
            {
                EntityQueryCache.Add(entity);
            }
        }

        int[] result = [.. EntityQueryCache];
        _queryCache[cacheKey] = result;
        return result;
    }

    public IEnumerable<int> GetEntitiesWithComponents<T1, T2, T3>()
        where T1 : Component
        where T2 : Component
        where T3 : Component
    {
        ArchetypeKey cacheKey = GetArchetypeKey(typeof(T1), typeof(T2), typeof(T3));

        if (_queryCache.TryGetValue(cacheKey, out int[]? cached))
        {
            return cached;
        }

        EntityQueryCache.Clear();
        IEnumerable<int> entities = GetEntitiesWithComponents<T1, T2>();

        foreach (int entity in entities)
        {
            if (GetComponent<T3>(entity) != null)
            {
                EntityQueryCache.Add(entity);
            }
        }

        int[] result = [.. EntityQueryCache];
        _queryCache[cacheKey] = result;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetEntityPosition(int entityId, Vector3 position) => _entityPositions[entityId] = position;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 GetEntityPosition(int entityId) => _entityPositions[entityId];

    public void RemoveEntity(int entityId)
    {
        if (_entityComponents.TryGetValue(entityId, out List<Component>? components))
        {
            // Return components to pool
            foreach (Component component in components)
            {
                Type type = component.GetType();
                if (!_componentPools.TryGetValue(type, out Stack<Component>? pool))
                {
                    pool = new Stack<Component>();
                    _componentPools[type] = pool;
                }
                pool.Push(component);
            }

            // Remove from archetype
            ArchetypeKey archetype = _entityToArchetype[entityId];
            if (_entityArchetypes.TryGetValue(archetype, out HashSet<int>? entitySet))
            {
                entitySet.Remove(entityId);
            }
            _entityToArchetype.Remove(entityId);

            _entityComponents.Remove(entityId);
            _entityPositions.Remove(entityId);

            foreach (Dictionary<int, Component> typeComponents in _componentsByType.Values)
            {
                typeComponents.Remove(entityId);
            }

            // Invalidate query cache
            _queryCache.Clear();
        }
    }

    public void RemoveComponent<T>(int entityId) where T : Component
    {
        Type type = typeof(T);
        if (_componentsByType.TryGetValue(type, out Dictionary<int, Component>? components))
        {
            if (components.TryGetValue(entityId, out Component? component))
            {
                components.Remove(entityId);

                // Return to pool
                if (!_componentPools.TryGetValue(type, out Stack<Component>? pool))
                {
                    pool = new Stack<Component>();
                    _componentPools[type] = pool;
                }
                pool.Push(component);
            }
        }

        if (_entityComponents.TryGetValue(entityId, out List<Component>? entityComps))
        {
            entityComps.RemoveAll(c => c is T);
            UpdateEntityArchetype(entityId);
        }

        // Invalidate query cache
        _queryCache.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EntityExists(int entityId) => _entityComponents.ContainsKey(entityId);

    public T CreateComponent<T>() where T : Component, new()
    {
        Type type = typeof(T);
        if (_componentPools.TryGetValue(type, out Stack<Component>? pool) && pool.Count > 0)
        {
            return (T)pool.Pop();
        }
        return new T();
    }

    public void Dispose()
    {
        _entityComponents.Clear();
        _componentsByType.Clear();
        _componentPools.Clear();
        _entityPositions.Clear();
        _entityArchetypes.Clear();
        _entityToArchetype.Clear();
        _queryCache.Clear();
        EntityQueryCache.Clear();
    }
}

public readonly struct ArchetypeKey(Type[] types) : IEquatable<ArchetypeKey>
{
    public static readonly ArchetypeKey Empty = new([]);

    private readonly Type[] _types = types;
    private readonly int _hashCode = ComputeHashCode(types);

    private static int ComputeHashCode(Type[] types)
    {
        unchecked
        {
            int hash = 17;
            foreach (Type type in types)
            {
                hash = hash * 31 + type.GetHashCode();
            }
            return hash;
        }
    }

    public bool Equals(ArchetypeKey other)
    {
        if (_types.Length != other._types.Length)
        {
            return false;
        }

        for (int i = 0; i < _types.Length; i++)
        {
            if (_types[i] != other._types[i])
            {
                return false;
            }
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is ArchetypeKey other && Equals(other);

    public override int GetHashCode() => _hashCode;

    public static bool operator ==(ArchetypeKey left, ArchetypeKey right) => left.Equals(right);

    public static bool operator !=(ArchetypeKey left, ArchetypeKey right) => !(left == right);
}
