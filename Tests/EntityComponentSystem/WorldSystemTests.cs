using VoxelGame.EntityComponentSystem.Systems;
using VoxelGame.World;
using VoxelGame.World.Data;

namespace Tests.EntityComponentSystem
{
    /// <summary>
    /// Test suite for the WorldSystem class. Verifies chunk storage, retrieval,
    /// and management functionality in the voxel world.
    /// </summary>
    public class WorldSystemTests : IDisposable
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
        /// Cleans up resources used by the test class.
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose currently
        }

        /// <summary>
        /// Verifies that adding a chunk correctly stores its data and can be retrieved.
        /// </summary>
        [Fact]
        public void AddChunk_StoresChunkData()
        {
            var position = new ChunkPosition(0, 0, 0);
            var blocks = new BlockType[Chunk.SIZE, Chunk.HEIGHT, Chunk.SIZE];
            var chunkData = new ChunkData(blocks);

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
            var position = new ChunkPosition(0, 0, 0);
            var blocks = new BlockType[Chunk.SIZE, Chunk.HEIGHT, Chunk.SIZE];
            _worldSystem.AddChunk(position, new ChunkData(blocks));

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
            var position1 = new ChunkPosition(0, 0, 0);
            var position2 = new ChunkPosition(16, 0, 0);
            var blocks = new BlockType[Chunk.SIZE, Chunk.HEIGHT, Chunk.SIZE];

            _worldSystem.AddChunk(position1, new ChunkData(blocks));
            _worldSystem.AddChunk(position2, new ChunkData(blocks));

            var positions = _worldSystem.GetAllChunkPositions().ToList();

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
            var position = new ChunkPosition(0, 0, 0);

            var chunkData = _worldSystem.GetChunk(position);

            Assert.Null(chunkData);
        }

        /// <summary>
        /// Tests that adding a chunk to an existing position correctly updates
        /// the stored chunk data.
        /// </summary>
        [Fact]
        public void AddChunk_UpdatesExistingChunk()
        {
            var position = new ChunkPosition(0, 0, 0);
            var blocks1 = new BlockType[Chunk.SIZE, Chunk.HEIGHT, Chunk.SIZE];
            var blocks2 = new BlockType[Chunk.SIZE, Chunk.HEIGHT, Chunk.SIZE];
            var chunkData1 = new ChunkData(blocks1);
            var chunkData2 = new ChunkData(blocks2);

            _worldSystem.AddChunk(position, chunkData1);
            _worldSystem.AddChunk(position, chunkData2);

            Assert.Same(chunkData2, _worldSystem.GetChunk(position));
        }
    }
}
