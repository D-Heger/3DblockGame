using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

public class Camera(float width, float height, Vector3 position)
{
    private float _speed = 8f;
    private float _width = width;
    private float _height = height;
    private float _sensitivity = 360f;

    // position vars
    public Vector3 Position = position;

    private Vector3 _up = Vector3.UnitY;
    private Vector3 _front = -Vector3.UnitZ;
    private Vector3 _right = Vector3.UnitX;

    // --- view rotations ---
    private float _pitch;
    private float _yaw = -90.0f;

    private bool _firstMove = true;
    public Vector2 LastPosition;

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(Position, Position + _front, _up);
    }

    public Matrix4 GetProjectionMatrix()
    {
        return Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45.0f),
            _width / _height,
            0.1f,
            100.0f
        );
    }

    private void UpdateVectors()
    {
        if (_pitch > 89.0f)
        {
            _pitch = 89.0f;
        }
        if (_pitch < -89.0f)
        {
            _pitch = -89.0f;
        }

        _front.X =
            MathF.Cos(MathHelper.DegreesToRadians(_pitch))
            * MathF.Cos(MathHelper.DegreesToRadians(_yaw));
        _front.Y = MathF.Sin(MathHelper.DegreesToRadians(_pitch));
        _front.Z =
            MathF.Cos(MathHelper.DegreesToRadians(_pitch))
            * MathF.Sin(MathHelper.DegreesToRadians(_yaw));

        _front = Vector3.Normalize(_front);

        _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
        _up = Vector3.Normalize(Vector3.Cross(_right, _front));
    }

    public void InputController(KeyboardState kInput, MouseState mInput, FrameEventArgs e)
    {
        if (kInput.IsKeyDown(Keys.Escape))
        {
            Environment.Exit(0);
        }

        if (kInput.IsKeyDown(Keys.W))
        {
            Position += _front * _speed * (float)e.Time;
        }
        if (kInput.IsKeyDown(Keys.A))
        {
            Position -= _right * _speed * (float)e.Time;
        }
        if (kInput.IsKeyDown(Keys.S))
        {
            Position -= _front * _speed * (float)e.Time;
        }
        if (kInput.IsKeyDown(Keys.D))
        {
            Position += _right * _speed * (float)e.Time;
        }

        if (kInput.IsKeyDown(Keys.Space))
        {
            Position.Y += _speed * (float)e.Time;
        }
        if (kInput.IsKeyDown(Keys.LeftShift))
        {
            Position.Y -= _speed * (float)e.Time;
        }

        if (_firstMove)
        {
            LastPosition = new Vector2(mInput.X, mInput.Y);
            _firstMove = false;
        }
        else
        {
            var deltaX = mInput.X - LastPosition.X;
            var deltaY = mInput.Y - LastPosition.Y;
            LastPosition = new Vector2(mInput.X, mInput.Y);

            _yaw += deltaX * _sensitivity * (float)e.Time;
            _pitch -= deltaY * _sensitivity * (float)e.Time;
        }
        UpdateVectors();
    }

    public void Update(KeyboardState kInput, MouseState mInput, FrameEventArgs e)
    {
        InputController(kInput, mInput, e);
    }
}
