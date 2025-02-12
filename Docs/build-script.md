# Build Script Documentation

This document provides detailed information about the project's build scripts (`build.bat` for Windows and `build.sh` for Linux).

## Overview

The build scripts manage various build tasks for the 3D Block Game project. They provide a unified interface for common development tasks such as building, testing, and documentation generation across both Windows and Linux platforms.

## Architecture

### Command Dependencies
```
check    <-- restore <-- build <-- run
           |
           |-- test
           |
           |-- docs
```

### Error Handling
Both scripts use a unified error handling system:
- Exit Code 0: Success
- Exit Code 1: Missing requirements or tool failure
- Exit Code >1: Command-specific errors from underlying tools

Each command validates its prerequisites before execution and properly propagates errors up the chain.

## Prerequisites

### Windows
- .NET Core SDK installed and available in PATH
- DocFX installed (for documentation generation)
- Windows operating system
- Visual Studio 2019 or later (recommended for beginners)

### Linux
- .NET Core SDK installed and available in PATH
- DocFX installed (for documentation generation)
- mesa-utils package (for OpenGL verification)
- Bash shell

## Available Commands

The following commands are available in both Windows (`build.bat`) and Linux (`build.sh`):

### restore
- Restores all NuGet package dependencies
- Used automatically by other commands when needed
- Run this after checking out or pulling new changes
- Dependencies: Requires system check
- Usage:
  ```
  Windows: .\build.bat restore
  Linux:   ./build.sh restore
  ```

### build
- Builds the entire solution in Release configuration
- Automatically runs the restore command first
- Creates binaries in the project's bin directories
- Dependencies: Requires restore
- Usage:
  ```
  Windows: .\build.bat build
  Linux:   ./build.sh build
  ```

### run
- Builds and runs the game
- Ensures latest changes are compiled before running
- Executes the game from the VoxelGame project
- Dependencies: Requires build
- Usage:
  ```
  Windows: .\build.bat run
  Linux:   ./build.sh run
  ```

### test
- Runs the test suite
- Executes all tests in the Tests project
- Use this before committing changes
- Dependencies: Requires system check
- Usage:
  ```
  Windows: .\build.bat test
  Linux:   ./build.sh test
  ```

### clean
- Removes all build outputs
- Cleans the following directories:
  - VoxelGame/bin
  - VoxelGame/obj
  - Tests/bin
  - Tests/obj
- Use when you need a fresh build
- Dependencies: None
- Usage:
  ```
  Windows: .\build.bat clean
  Linux:   ./build.sh clean
  ```

### docs
- Generates API documentation using DocFX
- Serves the documentation locally at http://localhost:8080
- Dependencies: Requires system check and DocFX installation
- Usage:
  ```
  Windows: .\build.bat docs
  Linux:   ./build.sh docs
  ```

### check
- Validates system requirements
- Verifies tool installations
- Checks GPU capabilities
- Dependencies: None
- Usage:
  ```
  Windows: .\build.bat check
  Linux:   ./build.sh check
  ```

## Common Workflows

### First-Time Setup

Windows:
```batch
.\build.bat check   # Verify requirements
.\build.bat restore # Get dependencies
.\build.bat build   # Build the solution
```

Linux:
```bash
chmod +x build.sh        # Make script executable
./build.sh check        # Verify requirements
./build.sh restore      # Get dependencies
./build.sh build        # Build the solution
```

### Development Cycle
1. Make code changes
2. Run test command to verify changes:
   ```
   Windows: .\build.bat test
   Linux:   ./build.sh test
   ```
3. Run the game to test in-game:
   ```
   Windows: .\build.bat run
   Linux:   ./build.sh run
   ```

### Documentation Updates
1. Make documentation changes
2. Run docs command to preview:
   ```
   Windows: .\build.bat docs
   Linux:   ./build.sh docs
   ```
3. Access locally served documentation at http://localhost:8080

## Maintenance Guidelines

### Adding New Commands
1. Add the command handler:
   - Windows: Add label (e.g., `:newcommand`)
   - Linux: Add function and case statement
2. Add command routing in the main section
3. Add help text in the help section
4. Document dependencies in this file
5. Update the dependency diagram
6. Implement in both Windows and Linux scripts

### Modifying Error Handling
Windows:
- Use `SET EXIT_CODE=!ERRORLEVEL!` to capture tool errors
- Check `!EXIT_CODE!` before proceeding with dependent steps

Linux:
- Use `$?` to capture exit codes
- Store in `EXIT_CODE` variable
- Return exit codes from functions

## Troubleshooting

### Common Issues

1. **Build Fails**
   - Run clean command followed by build:
     ```
     Windows: build.bat clean && build.bat build
     Linux:   ./build.sh clean && ./build.sh build
     ```
   - Verify .NET Core SDK installation
   - Check error code for specific failure point

2. **Documentation Generation Fails**
   - Ensure DocFX is properly installed:
     ```
     dotnet tool install -g docfx
     ```
   - Check for valid markdown syntax
   - Verify docfx.json configuration

3. **Tests Fail**
   - Check test output for specific failures
   - Verify test dependencies are restored
   - Look for environment-specific issues

### Platform-Specific Issues

#### Windows
- If Visual Studio detection fails, you can still use VS Code
- Ensure PowerShell is available for GPU checks

#### Linux
- If OpenGL check fails, install mesa-utils:
  ```bash
  sudo apt install mesa-utils  # Ubuntu/Debian
  sudo dnf install glx-utils   # Fedora/RHEL
  ```
- Ensure build.sh has execute permissions:
  ```bash
  chmod +x build.sh
  ```