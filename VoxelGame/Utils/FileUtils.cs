using StbImageSharp;

namespace VoxelGame.Utils
{
    public static class FileUtils
    {
        public static string LoadVertexShader(string filePath)
        {
            return LoadShaderSource(filePath, "vert");
        }

        public static string LoadFragmentShader(string filePath)
        {
            return LoadShaderSource(filePath, "frag");
        }

        public static string LoadShaderSource(string filePath, string shaderType)
        {
            string shaderSource = "";

            try
            {
                using StreamReader reader = new($"./VoxelGame/Shaders/{filePath}.{shaderType}");
                shaderSource = reader.ReadToEnd();
            }
            catch (Exception)
            {
                // Silently fail and return empty shader source
                // This is intentional as shader loading errors are handled elsewhere
            }

            return shaderSource;
        }

        public static ImageResult LoadTexture(string filePath)
        {
            StbImage.stbi_set_flip_vertically_on_load(1);
            ImageResult texture = ImageResult.FromStream(
                File.OpenRead($"./VoxelGame/Textures/{filePath}.png"),
                ColorComponents.RedGreenBlueAlpha
            );

            return texture;
        }
    }
}
