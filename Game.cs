using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelGame.EntityComponentSystem;
using VoxelGame.EntityComponentSystem.Components;
using VoxelGame.EntityComponentSystem.Systems;
using VoxelGame.GraphicsPipeline;
using VoxelGame.World;
using VoxelGame.World.Data;

namespace VoxelGame
{
    public class Game : GameWindow
    {
        private int _width,
            _height;

        private double _time;
        private int _frames;

        private string _title = "3D Voxel Game";

        private EntityManager _entityManager;
        private RenderSystem _renderSystem;
        private InputSystem _inputSystem;

        public Game(int width, int height)
            : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            _width = width;
            _height = height;

            CenterWindow(new Vector2i(width, height));

            Title = _title;

            _time = 0;
            _frames = 0;
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            _width = e.Width;
            _height = e.Height;
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            _entityManager = new EntityManager();
            _renderSystem = new RenderSystem();
            _inputSystem = new InputSystem();

            // Create camera entity
            int cameraEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent(
                cameraEntity,
                new TransformComponent(new Vector3(8, 10, 8), Quaternion.Identity, Vector3.One)
            );
            _entityManager.AddComponent(cameraEntity, new CameraComponent());

            // Create chunk entity
            int chunkEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent(
                chunkEntity,
                new TransformComponent(Vector3.Zero, Quaternion.Identity, Vector3.One)
            );

            // Generate chunk mesh data
            ChunkMeshData chunkMeshData = ChunkGenerator.GenerateChunkMesh(Vector3.Zero);
            _entityManager.AddComponent(
                chunkEntity,
                new MeshComponent(chunkMeshData.Vertices, chunkMeshData.UVs, chunkMeshData.Indices)
            );

            // Add texture component
            Texture chunkTexture = new Texture("atlas");
            _entityManager.AddComponent(chunkEntity, new TextureComponent(chunkTexture));

            // Set cursor state
            CursorState = CursorState.Grabbed;
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            // Dispose of mesh buffers
            var meshEntities = _entityManager.GetEntitiesWithComponent<MeshComponent>();
            foreach (var entity in meshEntities)
            {
                var meshComponent = _entityManager.GetComponent<MeshComponent>(entity);
                meshComponent.Dispose();
            }

            // Dispose of textures
            var textureEntities = _entityManager.GetEntitiesWithComponent<TextureComponent>();
            foreach (var entity in textureEntities)
            {
                var textureComponent = _entityManager.GetComponent<TextureComponent>(entity);
                textureComponent.Texture.Dispose();
            }

            // Dispose of shader programs
            _renderSystem.Dispose();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.ClearColor(0.3f, 0.3f, 1f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Render system
            _renderSystem.Render(_entityManager);

            Context.SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            _time += args.Time;
            _frames++;

            if (_time >= 1.0)
            {
                Title = $"{_title} | FPS: {_frames}";
                _frames = 0;
                _time -= 1.0;
            }

            KeyboardState kInput = KeyboardState;
            MouseState mInput = MouseState;

            _inputSystem.Update(_entityManager, kInput, mInput, args);
        }
    }
}
