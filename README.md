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
- Visual Studio 2019 or later (recommended)

## Getting Started

1. Clone the repository
2. Open `VoxelGame.sln` in Visual Studio
3. Build the solution using `build.bat build`
4. Run the game using `build.bat run`

## Build Commands

The project includes a build script with the following commands:

- `build.bat restore` - Restore NuGet packages
- `build.bat build` - Build the solution
- `build.bat run` - Run the game
- `build.bat test` - Run the tests
- `build.bat clean` - Clean build outputs

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