using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using static FileUtils;

public class Texture
{
    public int ID;

    public Texture(string filepath)
    {
        ID = GL.GenTexture();

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, ID);

        GL.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.Repeat
        );
        GL.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.Repeat
        );
        GL.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Nearest
        );
        GL.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Nearest
        );

        var dirtTexture = LoadTexture(filepath);

        GL.TexImage2D(
            TextureTarget.Texture2D,
            0,
            PixelInternalFormat.Rgba,
            dirtTexture.Width,
            dirtTexture.Height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            dirtTexture.Data
        );

        Unbind();
    }

    public void Bind()
    {
        GL.BindTexture(TextureTarget.Texture2D, ID);
    }

    public static void Unbind()
    {
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Dispose()
    {
        GL.DeleteTexture(ID);
    }
}
