# Build Script Documentation

This document provides detailed information about the project's build script (`build.bat`).

## Overview

The build script is a Windows batch file that manages various build tasks for the 3D Block Game project. It provides a unified interface for common development tasks such as building, testing, and documentation generation.

## Architecture

### Command Dependencies
```
check    <-- restore <-- build <-- run
           |
           +-- test
           |
           +-- docs
```

### Error Handling
The script uses a unified error handling system:
- Exit Code 0: Success
- Exit Code 1: Missing requirements or tool failure
- Exit Code >1: Command-specific errors from underlying tools

Each command validates its prerequisites before execution and properly propagates errors up the chain.

## Prerequisites

- .NET Core SDK installed and available in PATH
- DocFX installed (for documentation generation)
- Windows operating system

## Available Commands

### `build.bat restore`
- Restores all NuGet package dependencies
- Used automatically by other commands when needed
- Run this after checking out or pulling new changes
- Dependencies: Requires system check

### `build.bat build`
- Builds the entire solution in Release configuration
- Automatically runs the restore command first
- Creates binaries in the project's bin directories
- Dependencies: Requires restore

### `build.bat run`
- Builds and runs the game
- Ensures latest changes are compiled before running
- Executes the game from the VoxelGame project
- Dependencies: Requires build

### `build.bat test`
- Runs the test suite
- Executes all tests in the Tests project
- Use this before committing changes
- Dependencies: Requires system check

### `build.bat clean`
- Removes all build outputs
- Cleans the following directories:
  - VoxelGame/bin
  - VoxelGame/obj
  - Tests/bin
  - Tests/obj
- Use when you need a fresh build
- Dependencies: None

### `build.bat docs`
- Generates API documentation using DocFX
- Serves the documentation locally at http://localhost:8080
- Dependencies: Requires system check and DocFX installation

### `build.bat check`
- Validates system requirements
- Verifies tool installations
- Checks GPU capabilities
- Dependencies: None

## Common Workflows

### First-Time Setup
```batch
.\build.bat check   # Verify requirements
.\build.bat restore # Get dependencies
.\build.bat build   # Build the solution
```

### Development Cycle
1. Make code changes
2. Run `build.bat test` to verify changes
3. Run `build.bat run` to test in-game

### Documentation Updates
1. Make documentation changes
2. Run `build.bat docs` to preview
3. Access locally served documentation

## Maintenance Guidelines

### Adding New Commands
1. Add the command handler label (e.g., `:newcommond`)
2. Add command routing in the main section
3. Add help text in the `:help` section
4. Document dependencies in this file
5. Update the dependency diagram

### Modifying Error Handling
- Use `SET EXIT_CODE=!ERRORLEVEL!` to capture tool errors
- Check `!EXIT_CODE!` before proceeding with dependent steps
- Add appropriate error messages using `echo Error: ...`

## Troubleshooting

### Common Issues

1. **Build Fails**
   - Run `build.bat clean` followed by `build.bat build`
   - Verify .NET Core SDK installation
   - Check error code for specific failure point

2. **Documentation Generation Fails**
   - Ensure DocFX is properly installed
   - Check for valid markdown syntax
   - Verify docfx.json configuration

3. **Tests Fail**
   - Check test output for specific failures
   - Verify test dependencies are restored
   - Look for environment-specific issues