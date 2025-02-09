using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using static VoxelGame.Utils.FileUtils;

namespace VoxelGame.GraphicsPipeline
{
    public class ShaderProgram
    {
        public int ID;
        private readonly Dictionary<string, int> _uniformLocations = new();

        public ShaderProgram(string vertexFilePath, string fragmentFilePath)
        {
            ID = GL.CreateProgram();

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, LoadVertexShader(vertexFilePath));
            GL.CompileShader(vertexShader);
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int vertexStatus);
            if (vertexStatus == 0)
                Console.WriteLine($"Vertex Shader Error: {GL.GetShaderInfoLog(vertexShader)}");

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, LoadFragmentShader(fragmentFilePath));
            GL.CompileShader(fragmentShader);
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int fragmentStatus);
            if (fragmentStatus == 0)
                Console.WriteLine($"Fragment Shader Error: {GL.GetShaderInfoLog(fragmentShader)}");

            GL.AttachShader(ID, vertexShader);
            GL.AttachShader(ID, fragmentShader);

            GL.LinkProgram(ID);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        public void SetVector3(string name, Vector3 value)
        {
            if (!_uniformLocations.TryGetValue(name, out int location))
            {
                location = GL.GetUniformLocation(ID, name);
                _uniformLocations[name] = location;
            }
            GL.Uniform3(location, value);
        }

        public void SetMatrix4(string name, Matrix4 value)
        {
            if (!_uniformLocations.TryGetValue(name, out int location))
            {
                location = GL.GetUniformLocation(ID, name);
                _uniformLocations[name] = location;
            }
            GL.UniformMatrix4(location, true, ref value);
        }

        public void SetInt(string name, int value)
        {
            if (!_uniformLocations.TryGetValue(name, out int location))
            {
                location = GL.GetUniformLocation(ID, name);
                _uniformLocations[name] = location;
            }
            GL.Uniform1(location, value);
        }

        public void Bind()
        {
            GL.UseProgram(ID);
        }

        public static void Unbind()
        {
            GL.UseProgram(0);
        }

        public void Dispose()
        {
            GL.DeleteProgram(ID);
        }
    }
}