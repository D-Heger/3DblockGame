using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using VoxelGame.EntityComponentSystem.Components;
using VoxelGame.GraphicsPipeline;
using VoxelGame.World;

namespace VoxelGame.EntityComponentSystem.Systems;

public class RenderSystem : System
{
    private readonly ShaderProgram _unlitShader;
    private readonly ShaderProgram _litShader;
    private ShaderProgram _currentShader;
    private bool _useLighting;
    private Vector3 _lightPosition = new(1000f, 1000f, 1000f); // Sun-like distant light
    private Vector3 _lightColor = new(1.0f, 1.0f, 0.9f); // Warm sunlight
    private readonly Frustum _frustum;
    private readonly int _width;
    private readonly int _height;
    private bool _wireframeMode;

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
        IEnumerable<int> cameraEntities = entityManager.GetEntitiesWithComponents<
            CameraComponent,
            TransformComponent
        >();
        if (!cameraEntities.Any())
        {
            return; // No camera to render from
        }

        int cameraEntity = cameraEntities.First();
        TransformComponent? cameraTransform = entityManager.GetComponent<TransformComponent>(cameraEntity);
        CameraComponent? cameraComponent = entityManager.GetComponent<CameraComponent>(cameraEntity);

        if (cameraTransform == null || cameraComponent == null)
        {
            return; // Required components are missing
        }

        Matrix4 view = cameraComponent.GetViewMatrix(cameraTransform.Position);
        Matrix4 projection = CameraComponent.GetProjectionMatrix((float)_width / _height);
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
        IEnumerable<int> renderEntities = entityManager.GetEntitiesWithComponents<
            MeshComponent,
            TransformComponent,
            TextureComponent
        >();

        Dictionary<int, List<int>> textureGroups = [];
        foreach (int entity in renderEntities)
        {
            TextureComponent? tex = entityManager.GetComponent<TextureComponent>(entity);
            if (tex?.Texture != null)
            {
                int texId = tex.Texture.ID;
                if (!textureGroups.TryGetValue(texId, out List<int>? group))
                {
                    group = [];
                    textureGroups[texId] = group;
                }
                group.Add(entity);
            }
        }

        foreach (KeyValuePair<int, List<int>> group in textureGroups)
        {
            // Bind texture once per group
            GL.BindTexture(TextureTarget.Texture2D, group.Key);
            _currentShader.SetInt("texture0", 0); // Set texture unit

            foreach (int entity in group.Value)
            {
                MeshComponent? meshComponent = entityManager.GetComponent<MeshComponent>(entity);
                TransformComponent? transformComponent = entityManager.GetComponent<TransformComponent>(entity);

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
