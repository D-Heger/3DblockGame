# Getting Started

## Setting Up the Development Environment

1. **Prerequisites**
   - Install .NET Core SDK
   - Make sure you have an OpenGL-compatible graphics card
   - Install Visual Studio 2019 or later (recommended)

2. **Building the Project**
   ```batch
   build.bat restore  # Restore NuGet packages
   build.bat build   # Build the solution
   ```

3. **Running the Game**
   ```batch 
   build.bat run
   ```

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

## Development Workflow

1. **Making Changes**
   - Make code changes in Visual Studio
   - Build using `build.bat build`
   - Run tests with `build.bat test`

2. **Debugging**
   - Use Visual Studio's debugger
   - Check debug output for OpenGL errors
   - Monitor memory usage with MemoryTracker

3. **Contributing**
   - Follow C# coding conventions
   - Add unit tests for new features
   - Update documentation as needed