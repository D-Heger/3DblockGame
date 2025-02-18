using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using VoxelGame.EntityComponentSystem.Components;
using VoxelGame.GraphicsPipeline;
using VoxelGame.World;

namespace VoxelGame.EntityComponentSystem.Systems
{
    public class RenderSystem : System
    {
        private ShaderProgram _unlitShader;
        private ShaderProgram _litShader;
        private ShaderProgram _currentShader;
        private bool _useLighting = false;
        private Vector3 _lightPosition = new(1000f, 1000f, 1000f); // Sun-like distant light
        private Vector3 _lightColor = new(1.0f, 1.0f, 0.9f); // Warm sunlight
        private Frustum _frustum;
        private int _width;
        private int _height;
        private bool _wireframeMode = false;

        public RenderSystem(int width, int height)
        {
            _width = width;
            _height = height;
            _unlitShader = new ShaderProgram("Default", "Default");
            _litShader = new ShaderProgram("Lit", "Lit");
            _currentShader = _unlitShader;
            _frustum = new Frustum();

            GL.Enable(EnableCap.DepthTest);
            GL.FrontFace(FrontFaceDirection.Cw);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
        }

        public void Render(EntityManager entityManager)
        {
            var cameraEntities = entityManager.GetEntitiesWithComponents<
                CameraComponent,
                TransformComponent
            >();
            if (!cameraEntities.Any())
            {
                return; // No camera to render from
            }

            int cameraEntity = cameraEntities.First();
            var cameraTransform = entityManager.GetComponent<TransformComponent>(cameraEntity);
            var cameraComponent = entityManager.GetComponent<CameraComponent>(cameraEntity);

            if (cameraTransform == null || cameraComponent == null)
            {
                return; // Required components are missing
            }

            Matrix4 view = cameraComponent.GetViewMatrix(cameraTransform.Position);
            Matrix4 projection = cameraComponent.GetProjectionMatrix((float)_width / _height);
            Matrix4 viewProjection = view * projection;

            // Update frustum
            _frustum.UpdateFrustum(viewProjection);

            // Bind shader program once
            if (_currentShader == null)
            {
                return; // Shader not initialized
            }
            _currentShader.Bind();

            // Set common uniforms
            _currentShader.SetMatrix4("view", view);
            _currentShader.SetMatrix4("projection", projection);

            if (_useLighting)
            {
                _currentShader.SetVector3("lightPos", _lightPosition);
                _currentShader.SetVector3("lightColor", _lightColor);
                _currentShader.SetVector3("viewPos", cameraTransform.Position);
            }

            // Group entities by texture to minimize texture binds
            var renderEntities = entityManager.GetEntitiesWithComponents<
                MeshComponent,
                TransformComponent,
                TextureComponent
            >();
            var entitiesByTexture = renderEntities
                .Select(e => new
                {
                    Entity = e,
                    TextureComponent = entityManager.GetComponent<TextureComponent>(e),
                })
                .Where(x => x.TextureComponent?.Texture != null)
                .GroupBy(x => x.TextureComponent!.Texture.ID);

            foreach (var group in entitiesByTexture)
            {
                // Bind texture once per group
                GL.BindTexture(TextureTarget.Texture2D, group.Key);
                _currentShader.SetInt("texture0", 0); // Set texture unit

                foreach (var entityInfo in group)
                {
                    var meshComponent = entityManager.GetComponent<MeshComponent>(
                        entityInfo.Entity
                    );
                    var transformComponent = entityManager.GetComponent<TransformComponent>(
                        entityInfo.Entity
                    );

                    if (meshComponent == null || transformComponent == null)
                    {
                        continue; // Skip if required components are missing
                    }

                    // Frustum culling
                    Vector3 chunkPosition = transformComponent.Position;
                    int chunkSize = Chunk.SIZE;
                    int chunkHeight = Chunk.HEIGHT;
                    Vector3 min = chunkPosition;
                    Vector3 max = chunkPosition + new Vector3(chunkSize, chunkHeight, chunkSize);

                    if (!_frustum.IsBoxInsideFrustum(min, max))
                    {
                        continue;
                    }

                    // Set model matrix uniform
                    Matrix4 model = Matrix4.CreateTranslation(transformComponent.Position);
                    _currentShader.SetMatrix4("model", model);

                    // Bind VAO and draw mesh
                    meshComponent.SetupBuffers();
                    if (meshComponent.VAO != null)
                    {
                        meshComponent.VAO.Bind();
                        GL.DrawElements(
                            PrimitiveType.Triangles,
                            meshComponent.Indices.Count,
                            DrawElementsType.UnsignedInt,
                            0
                        );
                    }
                }
            }

            // Unbind for safety
            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            ShaderProgram.Unbind();
        }

        public void ToggleLighting()
        {
            _useLighting = !_useLighting;
            _currentShader = _useLighting ? _litShader : _unlitShader;
        }

        public void ToggleWireframe()
        {
            _wireframeMode = !_wireframeMode;
            if (_wireframeMode)
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            }
            else
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            }
        }

        public void Dispose()
        {
            _unlitShader.Dispose();
            _litShader.Dispose();
        }
    }
}
