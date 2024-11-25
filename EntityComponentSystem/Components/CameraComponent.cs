using OpenTK.Mathematics;

namespace VoxelGame.EntityComponentSystem.Components
{
    public class CameraComponent : Component
    {
        public float Speed = 8f;
        public float Sensitivity = 0.2f;
        public Vector3 Front = -Vector3.UnitZ;
        public Vector3 Up = Vector3.UnitY;
        public Vector3 Right = Vector3.UnitX;

        public float Pitch = 0f;
        public float Yaw = -90f;

        public bool FirstMove = true;
        public Vector2 LastMousePosition;

        public int ScreenWidth = 1280;
        public int ScreenHeight = 720;

        public CameraComponent() { }

        public Matrix4 GetViewMatrix(Vector3 position)
        {
            return Matrix4.LookAt(position, position + Front, Up);
        }
    }
}
