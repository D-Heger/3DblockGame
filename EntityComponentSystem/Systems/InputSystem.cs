using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelGame.EntityComponentSystem.Components;

namespace VoxelGame.EntityComponentSystem.Systems
{
    public class InputSystem : System
    {
        public void Update(
            EntityManager entityManager,
            KeyboardState kInput,
            MouseState mInput,
            FrameEventArgs args
        )
        {
            // Handle input for camera entities
            var cameraEntities = entityManager.GetEntitiesWithComponents<
                CameraComponent,
                TransformComponent
            >();

            foreach (var entity in cameraEntities)
            {
                var cameraComponent = entityManager.GetComponent<CameraComponent>(entity);
                var transformComponent = entityManager.GetComponent<TransformComponent>(entity);

                if (cameraComponent == null || transformComponent == null)
                {
                    Console.WriteLine("Camera entity missing components.");
                    continue;
                }

                if (kInput.IsKeyDown(Keys.Escape))
                {
                    Environment.Exit(0);
                }

                // Handle keyboard input for movement
                Vector3 moveDirection = Vector3.Zero;

                if (kInput.IsKeyDown(Keys.W))
                {
                    moveDirection += cameraComponent.Front;
                }
                if (kInput.IsKeyDown(Keys.S))
                {
                    moveDirection -= cameraComponent.Front;
                }
                if (kInput.IsKeyDown(Keys.A))
                {
                    moveDirection -= cameraComponent.Right;
                }
                if (kInput.IsKeyDown(Keys.D))
                {
                    moveDirection += cameraComponent.Right;
                }
                // Movement along the world's Up axis
                if (kInput.IsKeyDown(Keys.Space))
                {
                    moveDirection += Vector3.UnitY;
                }
                if (kInput.IsKeyDown(Keys.LeftShift))
                {
                    moveDirection -= Vector3.UnitY;
                }

                if (kInput.IsKeyDown(Keys.LeftControl))
                {
                    cameraComponent.Speed = 32.0f;
                }
                else
                {
                    cameraComponent.Speed = 8.0f;
                }

                if (moveDirection.LengthSquared > 0)
                {
                    moveDirection = Vector3.Normalize(moveDirection);
                    transformComponent.Position += moveDirection * cameraComponent.Speed * (float)args.Time;
                }

                // Handle mouse input for rotation
                HandleMouseInput(cameraComponent, mInput, args);
            }
        }

        private void HandleMouseInput(
            CameraComponent cameraComponent,
            MouseState mInput,
            FrameEventArgs args
        )
        {
            if (cameraComponent.FirstMove)
            {
                cameraComponent.LastMousePosition = new Vector2(mInput.X, mInput.Y);
                cameraComponent.FirstMove = false;
            }
            else
            {
                var deltaX = mInput.X - cameraComponent.LastMousePosition.X;
                var deltaY = mInput.Y - cameraComponent.LastMousePosition.Y;
                cameraComponent.LastMousePosition = new Vector2(mInput.X, mInput.Y);

                cameraComponent.Yaw += deltaX * cameraComponent.Sensitivity;
                cameraComponent.Pitch -= deltaY * cameraComponent.Sensitivity;

                if (cameraComponent.Pitch > 89.0f)
                    cameraComponent.Pitch = 89.0f;
                if (cameraComponent.Pitch < -89.0f)
                    cameraComponent.Pitch = -89.0f;

                UpdateVectors(cameraComponent);
            }
        }

        private void UpdateVectors(CameraComponent cameraComponent)
        {
            cameraComponent.Front.X =
                MathF.Cos(MathHelper.DegreesToRadians(cameraComponent.Pitch))
                * MathF.Cos(MathHelper.DegreesToRadians(cameraComponent.Yaw));
            cameraComponent.Front.Y = MathF.Sin(MathHelper.DegreesToRadians(cameraComponent.Pitch));
            cameraComponent.Front.Z =
                MathF.Cos(MathHelper.DegreesToRadians(cameraComponent.Pitch))
                * MathF.Sin(MathHelper.DegreesToRadians(cameraComponent.Yaw));

            cameraComponent.Front = Vector3.Normalize(cameraComponent.Front);

            cameraComponent.Right = Vector3.Normalize(Vector3.Cross(cameraComponent.Front, Vector3.UnitY));
            cameraComponent.Up = Vector3.Normalize(Vector3.Cross(cameraComponent.Right, cameraComponent.Front));
        }
    }
}