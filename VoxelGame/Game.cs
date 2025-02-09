using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelGame.EntityComponentSystem;
using VoxelGame.EntityComponentSystem.Components;
using VoxelGame.EntityComponentSystem.Systems;
using VoxelGame.GraphicsPipeline;
using VoxelGame.Utils;
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
        private WorldSystem _worldSystem;
        private ChunkGenerationSystem _chunkGenerationSystem;

        private Vector3 _lastPlayerChunkPosition;
        private int _viewDistance = 32;

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

            // Initialize systems
            _entityManager = new EntityManager();
            _renderSystem = new RenderSystem(_width, _height);
            _inputSystem = new InputSystem();
            _worldSystem = new WorldSystem();
            _chunkGenerationSystem = new ChunkGenerationSystem(_entityManager, _worldSystem);

            // Create camera entity
            int cameraEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent(
                cameraEntity,
                new TransformComponent(new Vector3(8, 10, 8), Quaternion.Identity, Vector3.One)
            );
            _entityManager.AddComponent(cameraEntity, new CameraComponent());
            CursorState = CursorState.Grabbed;

            // Generate initial chunks
            _chunkGenerationSystem.GenerateInitialChunks(ChunkPosition.Zero(), _viewDistance);

            _lastPlayerChunkPosition = Vector3.Zero;
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

            MemoryTracker.Update();
            var (current, average, peak) = MemoryTracker.GetMemoryStats();

            if (_time >= 1.0)
            {
                Title = $"{_title} | FPS: {_frames} | Entities: {_entityManager.EntityCount} | Memory (MB) - Current: {current}, Avg: {average}, Peak: {peak}";
                _frames = 0;
                _time -= 1.0;
            }

            KeyboardState kInput = KeyboardState;
            MouseState mInput = MouseState;

            _inputSystem.Update(_entityManager, kInput, mInput, args);

            if (kInput.IsKeyPressed(Keys.F3))
            {
                _renderSystem.ToggleWireframe();
            }

            if (kInput.IsKeyPressed(Keys.F4))
            {
                _renderSystem.ToggleLighting();
            }

            var cameraEntity = _entityManager.GetEntitiesWithComponent<CameraComponent>();
            var cameraTransform = _entityManager.GetComponent<TransformComponent>(cameraEntity.First());

            Vector3 playerChunkPosition = new(
                (int)(cameraTransform.Position.X / Chunk.SIZE) * Chunk.SIZE,
                0,
                (int)(cameraTransform.Position.Z / Chunk.SIZE) * Chunk.SIZE
            );

            _chunkGenerationSystem.Update();
            _chunkGenerationSystem.UpdateChunks(playerChunkPosition, _viewDistance);
        }
    }
}
