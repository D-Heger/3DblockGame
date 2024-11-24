using OpenTK.Mathematics;

namespace VoxelGame.EntityComponentSystem.Components
{
    public class TransformComponent(Vector3 position, Quaternion rotation, Vector3 scale)
        : Component
    {
        public Vector3 Position = position;
        public Quaternion Rotation = rotation;
        public Vector3 Scale = scale;
    }
}
