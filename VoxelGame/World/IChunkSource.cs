namespace VoxelGame.World;

/// <summary>
/// Read-only access to chunk block data registered in the world. Keeps
/// <see cref="ChunkGenerator"/> decoupled from ECS runtime types: meshing
/// only needs to look up neighboring chunks, never mutate the world.
/// </summary>
public interface IChunkSource
{
    public ChunkData? GetChunk(ChunkPosition chunkPosition);
}
