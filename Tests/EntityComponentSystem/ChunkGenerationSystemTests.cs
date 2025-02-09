using VoxelGame.EntityComponentSystem;
using VoxelGame.EntityComponentSystem.Systems;
using VoxelGame.EntityComponentSystem.Components;
using VoxelGame.World;
using VoxelGame.World.Data;
using VoxelGame.GraphicsPipeline;
using OpenTK.Mathematics;

namespace Tests;

public class ChunkGenerationSystemTests : IDisposable
{
    private class MockTextureProvider : ITextureProvider
    {
        public Texture GetTexture(string name) => new MockTexture();
    }

    private class MockTexture : Texture
    {
        public MockTexture() : base("") { }  // Pass empty string to base to prevent file loading
        public override void Bind() { }
        public override void Dispose() { }
    }

    private readonly EntityManager _entityManager;
    private readonly WorldSystem _worldSystem;
    private readonly ChunkGenerationSystem _chunkGenerationSystem;

    public ChunkGenerationSystemTests()
    {
        _entityManager = new EntityManager();
        _worldSystem = new WorldSystem();
        _chunkGenerationSystem = new ChunkGenerationSystem(_entityManager, _worldSystem, new MockTextureProvider());
    }

    public void Dispose()
    {
        _entityManager.Dispose();
    }

    [Fact]
    public void GenerateInitialChunks_EnqueuesCorrectChunks()
    {
        // Arrange
        var origin = new ChunkPosition(0, 0, 0);
        int radius = 1;
        int expectedChunkCount = (2 * radius + 1) * (2 * radius + 1); // 3x3 grid for radius 1

        // Act
        _chunkGenerationSystem.GenerateInitialChunks(origin, radius);
        
        // Process a few frames to allow chunk generation
        for (int i = 0; i < 10; i++)
        {
            _chunkGenerationSystem.Update();
            // Small delay to allow async processing
            Thread.Sleep(100);
        }

        // Assert
        var existingChunks = _worldSystem.GetAllChunkPositions().ToList();
        Assert.Equal(expectedChunkCount, existingChunks.Count);
    }

    [Fact]
    public void UpdateChunks_AddsAndRemovesChunksBasedOnPlayerPosition()
    {
        // Arrange
        var playerInitialPosition = new Vector3(0, 0, 0);
        int renderDistance = 1;

        // Act - Initial generation
        _chunkGenerationSystem.UpdateChunks(playerInitialPosition, renderDistance);
        
        // Process initial chunks
        for (int i = 0; i < 10; i++)
        {
            _chunkGenerationSystem.Update();
            Thread.Sleep(100);
        }

        var initialChunkCount = _worldSystem.GetAllChunkPositions().Count();

        // Move player far away (4 chunks in X and Z direction)
        var newPlayerPosition = new Vector3(Chunk.SIZE * 4, 0, Chunk.SIZE * 4);
        _chunkGenerationSystem.UpdateChunks(newPlayerPosition, renderDistance);

        // Process updates
        for (int i = 0; i < 10; i++)
        {
            _chunkGenerationSystem.Update();
            Thread.Sleep(100);
        }

        // Assert
        var newChunks = _worldSystem.GetAllChunkPositions().ToList();
        Assert.NotEmpty(newChunks); // Verify we have chunks
        Assert.DoesNotContain(new ChunkPosition(0, 0, 0), newChunks); // Old chunk should be removed
        
        // Verify new chunks are around the new player position
        var expectedChunk = new ChunkPosition(Chunk.SIZE * 4, 0, Chunk.SIZE * 4);
        Assert.Contains(expectedChunk, newChunks);
    }

    [Fact]
    public void ChunkGeneration_CreatesCorrectEntityComponents()
    {
        // Arrange
        var chunkPosition = new ChunkPosition(0, 0, 0);
        
        // Act
        _chunkGenerationSystem.GenerateInitialChunks(chunkPosition, 0); // Only generate one chunk
        
        // Process generation
        for (int i = 0; i < 5; i++)
        {
            _chunkGenerationSystem.Update();
            Thread.Sleep(100);
        }

        // Assert
        var entitiesWithMesh = _entityManager.GetEntitiesWithComponent<MeshComponent>().ToList();
        var entitiesWithTransform = _entityManager.GetEntitiesWithComponent<TransformComponent>().ToList();
        var entitiesWithTexture = _entityManager.GetEntitiesWithComponent<TextureComponent>().ToList();

        Assert.Single(entitiesWithMesh);
        Assert.Single(entitiesWithTransform);
        Assert.Single(entitiesWithTexture);
        
        // Verify the transform position matches the chunk position
        var transform = _entityManager.GetComponent<TransformComponent>(entitiesWithTransform[0]);
        Assert.Equal(chunkPosition.X, transform.Position.X);
        Assert.Equal(chunkPosition.Y, transform.Position.Y);
        Assert.Equal(chunkPosition.Z, transform.Position.Z);
    }

    [Fact]
    public void UpdateChunks_WithSamePosition_DoesNotRegenerateChunks()
    {
        // Arrange
        var playerPosition = new Vector3(0, 0, 0);
        int renderDistance = 1;

        // Act - Initial generation
        _chunkGenerationSystem.UpdateChunks(playerPosition, renderDistance);
        
        // Process initial chunks
        for (int i = 0; i < 5; i++)
        {
            _chunkGenerationSystem.Update();
            Thread.Sleep(50);
        }

        var initialChunks = _worldSystem.GetAllChunkPositions().ToList();

        // Update with same position
        _chunkGenerationSystem.UpdateChunks(playerPosition, renderDistance);
        
        // Process any potential updates
        for (int i = 0; i < 5; i++)
        {
            _chunkGenerationSystem.Update();
            Thread.Sleep(50);
        }

        // Assert
        var finalChunks = _worldSystem.GetAllChunkPositions().ToList();
        Assert.Equal(initialChunks.Count, finalChunks.Count);
        Assert.All(initialChunks, chunk => Assert.Contains(chunk, finalChunks));
    }

    [Fact]
    public void UpdateChunks_WithDifferentRenderDistance_UpdatesChunkCount()
    {
        // Arrange
        var playerPosition = new Vector3(0, 0, 0);
        int initialRenderDistance = 1;

        // Act - Initial generation
        _chunkGenerationSystem.UpdateChunks(playerPosition, initialRenderDistance);
        
        // Process initial chunks
        for (int i = 0; i < 5; i++)
        {
            _chunkGenerationSystem.Update();
            Thread.Sleep(50);
        }

        var initialChunkCount = _worldSystem.GetAllChunkPositions().Count();

        // Update with larger render distance
        int newRenderDistance = 2;
        _chunkGenerationSystem.UpdateChunks(playerPosition, newRenderDistance);
        
        // Process updates
        for (int i = 0; i < 10; i++)
        {
            _chunkGenerationSystem.Update();
            Thread.Sleep(50);
        }

        // Assert
        var finalChunkCount = _worldSystem.GetAllChunkPositions().Count();
        Assert.True(finalChunkCount > initialChunkCount);
    }

    [Fact]
    public void RemoveChunk_DisposesComponentsCorrectly()
    {
        // Arrange
        var chunkPosition = new ChunkPosition(0, 0, 0);
        _chunkGenerationSystem.GenerateInitialChunks(chunkPosition, 0);
        
        // Process generation
        for (int i = 0; i < 5; i++)
        {
            _chunkGenerationSystem.Update();
            Thread.Sleep(50);
        }

        var initialEntitiesWithComponents = _entityManager
            .GetEntitiesWithComponents<MeshComponent, TransformComponent, TextureComponent>()
            .ToList();

        // Act - Move player far away to trigger chunk removal
        _chunkGenerationSystem.UpdateChunks(new Vector3(Chunk.SIZE * 10, 0, Chunk.SIZE * 10), 1);
        
        // Process updates
        for (int i = 0; i < 5; i++)
        {
            _chunkGenerationSystem.Update();
            Thread.Sleep(50);
        }

        // Assert
        foreach (var entity in initialEntitiesWithComponents)
        {
            Assert.False(_entityManager.EntityExists(entity));
            Assert.Null(_entityManager.GetComponent<MeshComponent>(entity));
            Assert.Null(_entityManager.GetComponent<TransformComponent>(entity));
            Assert.Null(_entityManager.GetComponent<TextureComponent>(entity));
        }
    }

    [Fact]
    public void GenerateInitialChunks_WithLargeRadius_HandlesLoadCorrectly()
    {
        // Arrange
        var origin = new ChunkPosition(0, 0, 0);
        int radius = 3; // Larger radius to test concurrent generation
        int expectedChunkCount = (2 * radius + 1) * (2 * radius + 1);

        // Act
        _chunkGenerationSystem.GenerateInitialChunks(origin, radius);
        
        // Process generation with longer timeout due to larger area
        for (int i = 0; i < 20; i++)
        {
            _chunkGenerationSystem.Update();
            Thread.Sleep(100);
        }

        // Assert
        var chunks = _worldSystem.GetAllChunkPositions().ToList();
        Assert.Equal(expectedChunkCount, chunks.Count);

        // Verify chunk positions are within radius
        foreach (var chunk in chunks)
        {
            Assert.True(Math.Abs(chunk.X / Chunk.SIZE) <= radius);
            Assert.True(Math.Abs(chunk.Z / Chunk.SIZE) <= radius);
        }
    }
}