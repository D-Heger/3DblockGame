using VoxelGame.GraphicsPipeline;

namespace VoxelGame.EntityComponentSystem.Components
{
    public class TextureComponent(Texture texture) : Component
    {
        public Texture Texture = texture;
    }
}
