# Build Documentation

This project uses a `Makefile` for all build tasks.

## Prerequisites

- .NET SDK (10 or later) installed and available in PATH
- DocFX installed globally (for documentation generation): `dotnet tool install -g docfx`

## Targets

| Target              | Description                                 |
| ------------------- | ------------------------------------------- |
| `make` / `make run` | Build and run the game (default)            |
| `make build`        | Build the solution in Release configuration |
| `make test`         | Run the test suite                          |
| `make clean`        | Remove all build outputs                    |
| `make docs`         | Generate and serve documentation locally    |
| `make check`        | Verify .NET SDK is installed                |

## Dependency Chain

```
check → build → run
check → test
check → docs
```

Running `make` automatically checks for the .NET SDK before building.

## Common Workflows

### First-time setup
```bash
make build
```

### Development cycle
```bash
make test   # verify changes
make run    # run the game
```

### Clean rebuild
```bash
make clean && make build
```

### Documentation
```bash
make docs
```
Serves documentation at `http://localhost:8080`.

## Troubleshooting

- **"Error: .NET SDK not found"** — Install from https://dotnet.microsoft.com/download
- **"Error: docfx not found"** — Run `dotnet tool install -g docfx`
- **Stale build artifacts** — Run `make clean` then `make build`
