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

namespace VoxelGame;

/// <summary>
/// The main game class that manages the game window, systems, and game loop.
/// Inherits from OpenTK's GameWindow to handle window management and rendering.
/// </summary>
/// <remarks>
/// This class is responsible for:
/// - Initializing and managing core game systems (rendering, input, world, chunk generation)
/// - Managing the game window and OpenGL context
/// - Handling window events (resize, load, unload)
/// - Managing the game loop (update and render frames)
/// - Performance monitoring and memory tracking
/// </remarks>
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

    /// <summary>
    /// Initializes a new instance of the Game class with specified window dimensions.
    /// </summary>
    /// <param name="width">The width of the game window in pixels</param>
    /// <param name="height">The height of the game window in pixels</param>
    public Game(int width, int height)
        : base(GameWindowSettings.Default, NativeWindowSettings.Default)
    {
        _width = width;
        _height = height;

        CenterWindow(new Vector2i(width, height));
        Title = _title;
        _time = 0;
        _frames = 0;

        // Initialize core systems
        _entityManager = new EntityManager();
        _renderSystem = new RenderSystem(_width, _height);
        _inputSystem = new InputSystem();
        _worldSystem = new WorldSystem();
        _chunkGenerationSystem = new ChunkGenerationSystem(_entityManager, _worldSystem);
    }

    /// <summary>
    /// Handles window resize events by updating the viewport and internal dimensions.
    /// </summary>
    /// <param name="e">Contains information about the new window size</param>
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        _width = e.Width;
        _height = e.Height;
    }

    /// <summary>
    /// Initializes game systems and creates the initial game state when the window loads.
    /// Sets up the entity-component system, rendering system, input handling, world system,
    /// and chunk generation system. Also creates the initial camera entity and generates
    /// the starting chunks.
    /// </summary>
    protected override void OnLoad()
    {
        base.OnLoad();

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

    /// <summary>
    /// Cleans up resources when the window is closing.
    /// Disposes of mesh buffers, textures, and shader programs to prevent memory leaks.
    /// </summary>
    protected override void OnUnload()
    {
        base.OnUnload();

        // Dispose of mesh buffers
        var meshEntities = _entityManager.GetEntitiesWithComponent<MeshComponent>();
        foreach (var entity in meshEntities)
        {
            _entityManager.GetComponent<MeshComponent>(entity)?.Dispose();
        }

        // Dispose of textures
        var textureEntities = _entityManager.GetEntitiesWithComponent<TextureComponent>();
        foreach (var entity in textureEntities)
        {
            _entityManager.GetComponent<TextureComponent>(entity)?.Texture?.Dispose();
        }

        // Dispose of shader programs
        _renderSystem?.Dispose();
    }

    /// <summary>
    /// Handles the rendering of each frame.
    /// Clears the screen, updates the render system, and swaps the display buffers.
    /// </summary>
    /// <param name="args">Contains timing information for the frame</param>
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.ClearColor(0.3f, 0.3f, 1f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Render system
        _renderSystem.Render(_entityManager);

        Context.SwapBuffers();
    }

    /// <summary>
    /// Updates the game state each frame.
    /// Handles:
    /// - Performance monitoring and FPS counting
    /// - Memory tracking and statistics
    /// - Input processing
    /// - Debug controls (F3 for wireframe, F4 for lighting)
    /// - Camera position tracking
    /// - Chunk generation and updates based on player position
    /// </summary>
    /// <param name="args">Contains timing information for the frame</param>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        _time += args.Time;
        _frames++;

        MemoryTracker.Update();
        var (current, average, peak) = MemoryTracker.GetMemoryStats();

        if (_time >= 1.0)
        {
            Title =
                $"{_title} | FPS: {_frames} | Entities: {_entityManager.EntityCount} | Memory (MB) - Current: {current}, Avg: {average}, Peak: {peak}";
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

        var cameraEntities = _entityManager.GetEntitiesWithComponent<CameraComponent>();
        if (!cameraEntities.Any())
        {
            return; // No camera to update from
        }

        var cameraTransform = _entityManager.GetComponent<TransformComponent>(
            cameraEntities.First()
        );
        if (cameraTransform == null)
        {
            return; // No transform component found
        }

        Vector3 playerChunkPosition =
            new(
                (int)(cameraTransform.Position.X / Chunk.SIZE) * Chunk.SIZE,
                0,
                (int)(cameraTransform.Position.Z / Chunk.SIZE) * Chunk.SIZE
            );

        _chunkGenerationSystem?.Update();
        _chunkGenerationSystem?.UpdateChunks(playerChunkPosition, _viewDistance);
    }
}
