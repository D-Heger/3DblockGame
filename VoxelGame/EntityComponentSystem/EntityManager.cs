using OpenTK.Mathematics;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace VoxelGame.EntityComponentSystem;

public class EntityManager
{
    private int _nextEntityId = 0;
    private readonly Dictionary<int, List<Component>> _entityComponents = [];
    private readonly Dictionary<Type, Dictionary<int, Component>> _componentsByType = [];
    private readonly Dictionary<Type, Stack<Component>> _componentPools = [];
    private readonly Dictionary<int, Vector3> _entityPositions = [];

    // Archetype optimization
    private readonly Dictionary<string, HashSet<int>> _entityArchetypes = [];
    private readonly Dictionary<int, string> _entityToArchetype = [];

    // Query result caching
    private static readonly Dictionary<string, int[]> _queryCache = [];
    private static readonly int[] EmptyIntArray = [];
    private static readonly HashSet<int> EntityQueryCache = [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetArchetypeKey(params Type[] componentTypes)
    {
        Array.Sort(componentTypes, (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return string.Join(":", componentTypes.Select(t => t.Name));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CreateEntity()
    {
        int entityId = _nextEntityId++;
        _entityComponents[entityId] = new(4);
        _entityToArchetype[entityId] = "";
        return entityId;
    }

    public int EntityCount => _entityComponents.Count;

    public void AddComponent<T>(int entityId, T component) where T : Component
    {
        if (!_entityComponents.ContainsKey(entityId))
        {
            throw new Exception("Entity does not exist");
        }

        _entityComponents[entityId].Add(component);

        Type type = typeof(T);
        if (!_componentsByType.TryGetValue(type, out var componentDict))
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
        if (_entityComponents.TryGetValue(entityId, out var components))
        {
            // Remove from old archetype
            string oldArchetype = _entityToArchetype[entityId];
            if (_entityArchetypes.TryGetValue(oldArchetype, out var oldSet))
            {
                oldSet.Remove(entityId);
            }

            // Calculate new archetype
            var types = components.Select(c => c.GetType()).ToArray();
            string newArchetype = GetArchetypeKey(types);
            _entityToArchetype[entityId] = newArchetype;

            // Add to new archetype
            if (!_entityArchetypes.TryGetValue(newArchetype, out var entitySet))
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
        if (_componentsByType.TryGetValue(type, out var components) && 
            components.TryGetValue(entityId, out var component))
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
        string cacheKey = GetArchetypeKey(type);

        if (_queryCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        if (_componentsByType.TryGetValue(type, out var components))
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
        var entities = GetEntitiesWithComponent<T>();
        return entities.Any() ? entities.First() : -1;
    }

    public IEnumerable<int> GetEntitiesWithComponents<T1, T2>()
        where T1 : Component
        where T2 : Component
    {
        string cacheKey = GetArchetypeKey(typeof(T1), typeof(T2));
        
        if (_queryCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        EntityQueryCache.Clear();
        var entities1 = GetEntitiesWithComponent<T1>();
        
        foreach (var entity in entities1)
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
        string cacheKey = GetArchetypeKey(typeof(T1), typeof(T2), typeof(T3));
        
        if (_queryCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        EntityQueryCache.Clear();
        var entities = GetEntitiesWithComponents<T1, T2>();
        
        foreach (var entity in entities)
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
    public void SetEntityPosition(int entityId, Vector3 position)
    {
        _entityPositions[entityId] = position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 GetEntityPosition(int entityId)
    {
        return _entityPositions[entityId];
    }

    public void RemoveEntity(int entityId)
    {
        if (_entityComponents.TryGetValue(entityId, out var components))
        {
            // Return components to pool
            foreach (var component in components)
            {
                Type type = component.GetType();
                if (!_componentPools.TryGetValue(type, out var pool))
                {
                    pool = new Stack<Component>();
                    _componentPools[type] = pool;
                }
                pool.Push(component);
            }
            
            // Remove from archetype
            string archetype = _entityToArchetype[entityId];
            if (_entityArchetypes.TryGetValue(archetype, out var entitySet))
            {
                entitySet.Remove(entityId);
            }
            _entityToArchetype.Remove(entityId);
            
            _entityComponents.Remove(entityId);
            _entityPositions.Remove(entityId);
            
            foreach (var typeComponents in _componentsByType.Values)
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
        if (_componentsByType.TryGetValue(type, out var components))
        {
            if (components.TryGetValue(entityId, out var component))
            {
                components.Remove(entityId);
                
                // Return to pool
                if (!_componentPools.TryGetValue(type, out var pool))
                {
                    pool = new Stack<Component>();
                    _componentPools[type] = pool;
                }
                pool.Push(component);
            }
        }

        if (_entityComponents.TryGetValue(entityId, out var entityComps))
        {
            entityComps.RemoveAll(c => c is T);
            UpdateEntityArchetype(entityId);
        }

        // Invalidate query cache
        _queryCache.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EntityExists(int entityId)
    {
        return _entityComponents.ContainsKey(entityId);
    }

    public T CreateComponent<T>() where T : Component, new()
    {
        Type type = typeof(T);
        if (_componentPools.TryGetValue(type, out var pool) && pool.Count > 0)
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
