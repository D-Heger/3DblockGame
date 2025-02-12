# 3D Block Game

A voxel-based game engine written in C# using OpenTK and Entity Component System architecture.

See [Documentation](index.md) for more information.

## Features

- Procedural chunk generation
- Entity Component System (ECS) architecture
- OpenGL-based rendering pipeline
- Dynamic chunk loading/unloading
- Texture support
- Basic physics and collision detection

## Prerequisites

- .NET Core SDK
- OpenGL-compatible graphics card
- For Windows: Visual Studio 2019 or later (recommended for beginners)
- For Linux: mesa-utils package (for OpenGL verification)

## Getting Started

### Windows
1. Clone the repository
2. Open `VoxelGame.sln` in Visual Studio 
   (or open the folder in Visual Studio Code with C# extension if you're comfortable with that)
3. Run `build.bat check` to verify your environment
4. Run `build.bat build` to build and `build.bat run` to start the game

### Linux
1. Clone the repository
2. Run `chmod +x build.sh` to make the build script executable
3. Run `./build.sh check` to verify your environment
4. Run `./build.sh build` to build and `./build.sh run` to start the game

For detailed information about build commands, options, and troubleshooting, see [Build Script Documentation](Docs/build-script.md).

## Project Structure

- `VoxelGame/` - Main game engine and implementation
  - `EntityComponentSystem/` - ECS implementation
  - `GraphicsPipeline/` - OpenGL rendering pipeline
  - `World/` - World generation and chunk management
- `Tests/` - Unit tests
- `Docs/` - Documentation

## Architecture

The game uses an Entity Component System (ECS) architecture with the following key systems:

- `WorldSystem` - Manages chunks and world generation
- `ChunkGenerationSystem` - Handles procedural chunk generation
- `RenderSystem` - Manages OpenGL rendering pipeline
- `InputSystem` - Handles user input