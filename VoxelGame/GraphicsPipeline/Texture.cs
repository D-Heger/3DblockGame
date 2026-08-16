using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using static VoxelGame.Utils.FileUtils;

namespace VoxelGame.GraphicsPipeline;

public class Texture
{
    public int ID;

    public Texture(string filepath)
    {
        if (string.IsNullOrEmpty(filepath))
        {
            return;  // Allow empty constructor for mocking
        }

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

        ImageResult blockTexture = LoadTexture(filepath);
        if (blockTexture == null)
        {
            Console.WriteLine("Texture loading failed!");
            return;
        }

        GL.TexImage2D(
            TextureTarget.Texture2D,
            0,
            PixelInternalFormat.Rgba,
            blockTexture.Width,
            blockTexture.Height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            blockTexture.Data
        );

        Unbind();
    }

    public virtual void Bind() => GL.BindTexture(TextureTarget.Texture2D, ID);

    public static void Unbind() => GL.BindTexture(TextureTarget.Texture2D, 0);

    public virtual void Dispose() => GL.DeleteTexture(ID);
}
