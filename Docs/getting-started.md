# Getting Started

## Prerequisites

1. **System Requirements**
   - .NET Core SDK
   - OpenGL-compatible graphics card
   - For Windows: Visual Studio 2019 or later (recommended for beginners)
   - For Linux: mesa-utils package

2. **Building and Running**
   See the [Build Script Documentation](build-script.md) for detailed instructions on building and running the game.

## Project Organization

The solution consists of two main projects:
- `VoxelGame` - The main game engine
- `Tests` - Unit tests for core functionality

### Key Directories

- `VoxelGame/`
  - `EntityComponentSystem/` - Core ECS implementation
  - `GraphicsPipeline/` - OpenGL rendering code
  - `World/` - World generation and chunk management
  - `Utils/` - Helper utilities

### Configuration

Default game settings can be modified through:
- Window size in the Game constructor
- Chunk size and render distance in WorldSystem
- Block types and textures in the TextureData class

## Development Guidelines

### Code Style
- Follow C# coding conventions
- Maintain consistent naming patterns with existing code
- Document public APIs using XML comments

### Testing
- Add unit tests for new features
- Tests should be focused and descriptive
- Use meaningful test names that describe the scenario

### Documentation
- Update API documentation for new public members
- Keep README.md up to date with major changes
- Document any non-obvious implementation details

### Performance Considerations
- Use the MemoryTracker for memory profiling
- Check OpenGL debug output for rendering issues
- Consider chunk loading/unloading impact