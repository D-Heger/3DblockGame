using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

public class InputSystem
{
    public void Update(EntityManager entityManager, KeyboardState kInput, MouseState mInput, FrameEventArgs args)
    {
        // Handle input for camera entities
        var cameraEntities = entityManager.GetEntitiesWithComponents<CameraComponent, TransformComponent>();

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
            if (kInput.IsKeyDown(Keys.W))
            {
                transformComponent.Position += cameraComponent.Front * cameraComponent.Speed * (float)args.Time;
            }
            if (kInput.IsKeyDown(Keys.S))
            {
                transformComponent.Position -= cameraComponent.Front * cameraComponent.Speed * (float)args.Time;
            }
            if (kInput.IsKeyDown(Keys.A))
            {
                transformComponent.Position -= cameraComponent.Right * cameraComponent.Speed * (float)args.Time;
            }
            if (kInput.IsKeyDown(Keys.D))
            {
                transformComponent.Position += cameraComponent.Right * cameraComponent.Speed * (float)args.Time;
            }
            if (kInput.IsKeyDown(Keys.Space))
            {
                transformComponent.Position += cameraComponent.Up * cameraComponent.Speed * (float)args.Time;
            }
            if (kInput.IsKeyDown(Keys.LeftShift))
            {
                transformComponent.Position -= cameraComponent.Up * cameraComponent.Speed * (float)args.Time;
            }

            // Handle mouse input for rotation
            HandleMouseInput(cameraComponent, mInput, args);
        }
    }

    private void HandleMouseInput(CameraComponent cameraComponent, MouseState mInput, FrameEventArgs args)
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

            UpdateCameraVectors(cameraComponent);
        }
    }

    private void UpdateCameraVectors(CameraComponent cameraComponent)
    {
        Vector3 front;
        front.X = MathF.Cos(MathHelper.DegreesToRadians(cameraComponent.Pitch)) * MathF.Cos(MathHelper.DegreesToRadians(cameraComponent.Yaw));
        front.Y = MathF.Sin(MathHelper.DegreesToRadians(cameraComponent.Pitch));
        front.Z = MathF.Cos(MathHelper.DegreesToRadians(cameraComponent.Pitch)) * MathF.Sin(MathHelper.DegreesToRadians(cameraComponent.Yaw));
        cameraComponent.Front = Vector3.Normalize(front);

        cameraComponent.Right = Vector3.Normalize(Vector3.Cross(cameraComponent.Front, Vector3.UnitY));
        cameraComponent.Up = Vector3.Normalize(Vector3.Cross(cameraComponent.Right, cameraComponent.Front));
    }
}