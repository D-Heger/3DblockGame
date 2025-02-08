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
            catch (Exception e)
            {
                //Console.WriteLine("Failed to load shader source file: " + e.Message);
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
