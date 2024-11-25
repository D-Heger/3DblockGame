using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using VoxelGame.EntityComponentSystem.Components;
using VoxelGame.GraphicsPipeline;
using VoxelGame.World;

namespace VoxelGame.EntityComponentSystem.Systems
{
    public class RenderSystem : System
    {
        private ShaderProgram _shaderProgram;
        private Frustum _frustum;
        private int _width;
        private int _height;

        public RenderSystem(int width, int height)
        {
            _width = width;
            _height = height;
            _shaderProgram = new ShaderProgram("Default", "Default");
            _frustum = new Frustum();

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
            Matrix4 projection = cameraComponent.GetProjectionMatrix((float)_width / _height);
            Matrix4 viewProjection = view * projection;

            // Update frustum
            _frustum.UpdateFrustum(viewProjection);

            // Bind shader program once
            _shaderProgram.Bind();

            // Set common uniforms once
            int viewLocation = GL.GetUniformLocation(_shaderProgram.ID, "view");
            int projectionLocation = GL.GetUniformLocation(_shaderProgram.ID, "projection");

            GL.UniformMatrix4(viewLocation, true, ref view);
            GL.UniformMatrix4(projectionLocation, true, ref projection);

            // Group entities by texture to minimize texture binds
            var renderEntities = entityManager.GetEntitiesWithComponents<
                MeshComponent,
                TransformComponent,
                TextureComponent
            >();
            var entitiesByTexture = renderEntities.GroupBy(e =>
                entityManager.GetComponent<TextureComponent>(e).Texture.ID
            );

            foreach (var group in entitiesByTexture)
            {
                // Bind texture once per group
                int textureID = group.Key;
                GL.BindTexture(TextureTarget.Texture2D, textureID);

                foreach (var entity in group)
                {
                    var meshComponent = entityManager.GetComponent<MeshComponent>(entity);
                    var transformComponent = entityManager.GetComponent<TransformComponent>(entity);

                    // Frustum culling
                    Vector3 chunkPosition = transformComponent.Position;
                    int chunkSize = Chunk.SIZE;
                    int chunkHeight = Chunk.HEIGHT;
                    Vector3 min = chunkPosition;
                    Vector3 max = chunkPosition + new Vector3(chunkSize, chunkHeight, chunkSize);

                    if (!_frustum.IsBoxInsideFrustum(min, max))
                    {
                        continue; // Skip rendering this chunk
                    }

                    // Set model matrix uniform
                    Matrix4 model = Matrix4.CreateTranslation(transformComponent.Position);
                    int modelLocation = GL.GetUniformLocation(_shaderProgram.ID, "model");
                    GL.UniformMatrix4(modelLocation, true, ref model);

                    // Bind VAO and draw mesh
                    meshComponent.SetupBuffers();
                    meshComponent.VAO.Bind();
                    GL.DrawElements(
                        PrimitiveType.Triangles,
                        meshComponent.Indices.Count,
                        DrawElementsType.UnsignedInt,
                        0
                    );
                }
            }

            // Unbind for safety
            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            ShaderProgram.Unbind();
        }

        public void Dispose()
        {
            _shaderProgram.Dispose();
        }
    }
}
