# 3DblockGame

## Build Commands

This project supports building via both `make` and `build.bat`. Choose the appropriate command based on your operating system.

### Using Make (Linux/MacOS)

```bash
make [command]
```

### Using Build.bat (Windows)

```bash
./build.bat [command]
```

### Available Commands

| Command  | Description                                                           |
|----------|-----------------------------------------------------------------------|
| restore  | Restores all NuGet package dependencies                                |
| build    | Builds the solution in Release configuration (includes restore)        |
| run      | Builds and runs the game (includes build)                             |
| test     | Executes all project tests                                            |
| clean    | Removes all build artifacts, bin/obj directories, and documentation    |
| docs     | Generates documentation using Natural Docs and serves via localhost    |
| help     | Displays available commands                                            |

## Documentation

The project uses Natural Docs for documentation generation. After running the `docs` command, the documentation will be:
1. Generated in the `Docs` directory
2. Automatically opened in your default web browser at `http://localhost:8000`

To clean up documentation files, use the `clean` command.
