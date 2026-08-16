using VoxelGame.EntityComponentSystem.Systems;
using VoxelGame.World;

namespace Tests.EntityComponentSystem;

/// <summary>
/// Test suite for the WorldSystem class. Verifies chunk storage, retrieval,
/// and management functionality in the voxel world.
/// </summary>
public class WorldSystemTests
{
    private readonly WorldSystem _worldSystem;

    /// <summary>
    /// Initializes a new instance of the WorldSystemTests class.
    /// Sets up a fresh WorldSystem for each test.
    /// </summary>
    public WorldSystemTests()
    {
        _worldSystem = new WorldSystem();
    }

    /// <summary>
    /// Verifies that adding a chunk correctly stores its data and can be retrieved.
    /// </summary>
    [Fact]
    public void AddChunk_StoresChunkData()
    {
        ChunkPosition position = new(0, 0);
        ChunkData chunkData = new();

        _worldSystem.AddChunk(position, chunkData);

        Assert.True(_worldSystem.ChunkExists(position));
        Assert.Same(chunkData, _worldSystem.GetChunk(position));
    }

    /// <summary>
    /// Verifies that removing a chunk properly eliminates it from the world system.
    /// </summary>
    [Fact]
    public void RemoveChunk_RemovesChunkData()
    {
        ChunkPosition position = new(0, 0);
        _worldSystem.AddChunk(position, new ChunkData());

        _worldSystem.RemoveChunk(position);

        Assert.False(_worldSystem.ChunkExists(position));
        Assert.Null(_worldSystem.GetChunk(position));
    }

    /// <summary>
    /// Tests that getting all chunk positions returns the correct set of positions
    /// for all chunks in the world.
    /// </summary>
    [Fact]
    public void GetAllChunkPositions_ReturnsCorrectPositions()
    {
        ChunkPosition position1 = new(0, 0);
        ChunkPosition position2 = new(1, 0);

        _worldSystem.AddChunk(position1, new ChunkData());
        _worldSystem.AddChunk(position2, new ChunkData());

        List<ChunkPosition> positions = [.. _worldSystem.GetAllChunkPositions()];

        Assert.Equal(2, positions.Count);
        Assert.Contains(position1, positions);
        Assert.Contains(position2, positions);
    }

    /// <summary>
    /// Verifies that attempting to get a non-existent chunk returns null.
    /// </summary>
    [Fact]
    public void GetChunk_NonExistentPosition_ReturnsNull()
    {
        ChunkPosition position = new(0, 0);

        ChunkData? chunkData = _worldSystem.GetChunk(position);

        Assert.Null(chunkData);
    }

    /// <summary>
    /// Tests that adding a chunk to an existing position correctly updates
    /// the stored chunk data.
    /// </summary>
    [Fact]
    public void AddChunk_UpdatesExistingChunk()
    {
        ChunkPosition position = new(0, 0);
        ChunkData chunkData1 = new();
        ChunkData chunkData2 = new();

        _worldSystem.AddChunk(position, chunkData1);
        _worldSystem.AddChunk(position, chunkData2);

        Assert.Same(chunkData2, _worldSystem.GetChunk(position));
    }

    /// <summary>
    /// Verifies chunk-space key semantics: FromWorld floors block coordinates
    /// into chunk units, including for negative coordinates.
    /// </summary>
    [Fact]
    public void FromWorld_FloorsBlockCoordinatesToChunkSpace()
    {
        Assert.Equal(new ChunkPosition(0, 0), ChunkPosition.FromWorld(8, 8));
        Assert.Equal(new ChunkPosition(1, -1), ChunkPosition.FromWorld(16, -1));
        Assert.Equal(new ChunkPosition(-2, 3), ChunkPosition.FromWorld(-32, 48));
    }
}
