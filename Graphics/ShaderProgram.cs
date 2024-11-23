using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using static FileUtils;

public class ShaderProgram
{
    public int ID;

    public ShaderProgram(string vertexFilePath, string fragmentFilePath)
    {
        ID = GL.CreateProgram();

        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, LoadVertexShader(vertexFilePath));
        GL.CompileShader(vertexShader);

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, LoadFragmentShader(fragmentFilePath));
        GL.CompileShader(fragmentShader);

        GL.AttachShader(ID, vertexShader);
        GL.AttachShader(ID, fragmentShader);

        GL.LinkProgram(ID);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
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
        GL.DeleteShader(ID);
    }
}
