using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using VoxelGame.EntityComponentSystem.Components;
using VoxelGame.GraphicsPipeline;

namespace VoxelGame.EntityComponentSystem.Systems
{
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
            var cameraEntities = entityManager.GetEntitiesWithComponents<
                CameraComponent,
                TransformComponent
            >();
            if (!cameraEntities.Any())
            {
                Console.WriteLine("No camera entity found.");
                return;
            }

            int cameraEntity = cameraEntities.First();
            var cameraTransform = entityManager.GetComponent<TransformComponent>(cameraEntity);
            var cameraComponent = entityManager.GetComponent<CameraComponent>(cameraEntity);

            Matrix4 view = cameraComponent.GetViewMatrix(cameraTransform.Position);
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45.0f),
                1280f / 720f,
                0.1f,
                100f
            );

            // Get entities with MeshComponent and TransformComponent
            var renderEntities = entityManager.GetEntitiesWithComponents<
                MeshComponent,
                TransformComponent
            >();

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
                GL.DrawElements(
                    PrimitiveType.Triangles,
                    meshComponent.Indices.Count,
                    DrawElementsType.UnsignedInt,
                    0
                );

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
}
