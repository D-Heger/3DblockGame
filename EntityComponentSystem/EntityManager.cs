using OpenTK.Mathematics;

namespace VoxelGame.EntityComponentSystem
{
    public class EntityManager
    {
        private int _nextEntityId = 0;
        private Dictionary<int, List<Component>> _entityComponents = [];
        private Dictionary<Type, Dictionary<int, Component>> _componentsByType = [];

        public Dictionary<int, Vector3> EntityPositions = [];

        public int CreateEntity()
        {
            int entityId = _nextEntityId++;
            _entityComponents[entityId] = [];
            return entityId;
        }

        public int EntityCount => _entityComponents.Count;

        public void AddComponent<T>(int entityId, T component)
            where T : Component
        {
            if (!_entityComponents.ContainsKey(entityId))
            {
                throw new Exception("Entity does not exist");
            }

            _entityComponents[entityId].Add(component);

            Type type = typeof(T);
            if (!_componentsByType.ContainsKey(type))
            {
                _componentsByType[type] = [];
            }
            _componentsByType[type][entityId] = component;
        }

        public T GetComponent<T>(int entityId)
            where T : Component
        {
            if (!_entityComponents.ContainsKey(entityId))
            {
                throw new Exception("Entity does not exist");
            }

            foreach (var comp in _entityComponents[entityId])
            {
                if (comp is T t)
                    return t;
            }
            return null;
        }

        public IEnumerable<int> GetEntitiesWithComponent<T>()
            where T : Component
        {
            Type type = typeof(T);
            if (_componentsByType.ContainsKey(type))
            {
                return _componentsByType[type].Keys;
            }
            else
            {
                return [];
            }
        }

        public int GetEntityWithComponent<T>()
            where T : Component
        {
            Type type = typeof(T);
            if (_componentsByType.ContainsKey(type))
            {
                return _componentsByType[type].Keys.First();
            }
            else
            {
                return -1;
            }
        }

        public IEnumerable<int> GetEntitiesWithComponents<T1, T2>()
            where T1 : Component
            where T2 : Component
        {
            var entities1 = GetEntitiesWithComponent<T1>();
            var entities2 = GetEntitiesWithComponent<T2>();
            return entities1.Intersect(entities2);
        }

        public IEnumerable<int> GetEntitiesWithComponents<T1, T2, T3>()
            where T1 : Component
            where T2 : Component
            where T3 : Component
        {
            var entities1 = GetEntitiesWithComponent<T1>();
            var entities2 = GetEntitiesWithComponent<T2>();
            var entities3 = GetEntitiesWithComponent<T3>();
            return entities1.Intersect(entities2).Intersect(entities3);
        }

        public void SetEntityPosition(int entityID, Vector3 position)
        {
            EntityPositions[entityID] = position;
        }

        public Vector3 GetEntityPosition(int entityID)
        {
            return EntityPositions[entityID];
        }

        public void RemoveEntity(int entityID)
        {
            _entityComponents.Remove(entityID);
            foreach (var components in _componentsByType.Values)
            {
                components.Remove(entityID);
            }
        }

        public bool EntityExists(int entityID)
        {
            return _entityComponents.ContainsKey(entityID);
        }
    }
}
