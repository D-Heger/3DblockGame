using OpenTK.Graphics.OpenGL4;
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

public class RenderSystem
{
    private ShaderProgram _shaderProgram;

    public RenderSystem()
    {
        _shaderProgram = new ShaderProgram("Default", "Default");
        GL.Enable(EnableCap.DepthTest);
        GL.FrontFace(FrontFaceDirection.Cw);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Back);
    }

    public void Render(EntityManager entityManager)
    {
        // Get camera entity
        var cameraEntities = entityManager.GetEntitiesWithComponents<CameraComponent, TransformComponent>();
        if (!cameraEntities.Any())
        {
            Console.WriteLine("No camera entity found.");
            return;
        }

        int cameraEntity = cameraEntities.First();
        var cameraTransform = entityManager.GetComponent<TransformComponent>(cameraEntity);
        var cameraComponent = entityManager.GetComponent<CameraComponent>(cameraEntity);

        Matrix4 view = cameraComponent.GetViewMatrix(cameraTransform.Position);
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45.0f), 1280f / 720f, 0.1f, 100f);

        // Get entities with MeshComponent and TransformComponent
        var renderEntities = entityManager.GetEntitiesWithComponents<MeshComponent, TransformComponent>();

        foreach (var entity in renderEntities)
        {
            var meshComponent = entityManager.GetComponent<MeshComponent>(entity);
            var transformComponent = entityManager.GetComponent<TransformComponent>(entity);
            var textureComponent = entityManager.GetComponent<TextureComponent>(entity);

            // Set up model matrix
            Matrix4 model = Matrix4.CreateTranslation(transformComponent.Position);

            // Bind shader program
            _shaderProgram.Bind();

            // Set uniforms
            int modelLocation = GL.GetUniformLocation(_shaderProgram.ID, "model");
            int viewLocation = GL.GetUniformLocation(_shaderProgram.ID, "view");
            int projectionLocation = GL.GetUniformLocation(_shaderProgram.ID, "projection");

            GL.UniformMatrix4(modelLocation, true, ref model);
            GL.UniformMatrix4(viewLocation, true, ref view);
            GL.UniformMatrix4(projectionLocation, true, ref projection);

            // Bind texture
            textureComponent.Texture.Bind();

            // Bind VAO and draw mesh
            meshComponent.SetupBuffers();
            meshComponent.VAO.Bind();
            GL.DrawElements(PrimitiveType.Triangles, meshComponent.Indices.Count, DrawElementsType.UnsignedInt, 0);

            // Unbind for safety
            GL.BindVertexArray(0);
            Texture.Unbind();
            ShaderProgram.Unbind();
        }
    }

    public void Dispose()
    {
        _shaderProgram.Dispose();
    }
}