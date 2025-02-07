using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
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

    public Game(int width, int height)
        : base(GameWindowSettings.Default, NativeWindowSettings.Default)
    {
        _width = width;
        _height = height;

        CenterWindow(new Vector2i(width, height));
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

        _program = new ShaderProgram("Default", "Default");

        GL.Enable(EnableCap.DepthTest);
        GL.FrontFace(FrontFaceDirection.Cw);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Back);

        _camera = new Camera(_width, _height, new Vector3(8, 10, 8)); // Near the center of the first chunk

        CursorState = CursorState.Grabbed;

        _chunk = new Chunk(Vector3.Zero);
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        _chunk.Dispose();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GL.ClearColor(0.3f, 0.3f, 1f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        Matrix4 model = Matrix4.Identity;
        Matrix4 view = _camera.GetViewMatrix();
        Matrix4 projection = _camera.GetProjectionMatrix();

        int modelLocation = GL.GetUniformLocation(_program.ID, "model");
        int viewLocation = GL.GetUniformLocation(_program.ID, "view");
        int projectionLocation = GL.GetUniformLocation(_program.ID, "projection");

        GL.UniformMatrix4(modelLocation, true, ref model);
        GL.UniformMatrix4(viewLocation, true, ref view);
        GL.UniformMatrix4(projectionLocation, true, ref projection);
        //Console.WriteLine($"Model: {model}, View: {view}, Projection: {projection}");

        _chunk.RenderChunk(_program);

        Context.SwapBuffers();
        base.OnRenderFrame(args);
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

        base.OnUpdateFrame(args);
        _camera.Update(kInput, mInput, args);
    }
}
