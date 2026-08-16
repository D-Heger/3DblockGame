.PHONY: run build test clean docs check format format-style format-analyzers format-whitespace format-check

run: build
	dotnet run --project VoxelGame/VoxelGame.csproj --configuration Release

build: check
	dotnet build VoxelGame.sln --configuration Release

test: check
	dotnet test Tests/Tests.csproj

clean:
	dotnet clean VoxelGame.sln
	rm -rf VoxelGame/bin VoxelGame/obj Tests/bin Tests/obj

docs: check
	@export PATH="$$HOME/.dotnet/tools:$$PATH"; command -v docfx >/dev/null 2>&1 || { echo "Error: docfx not found. Install: dotnet tool install -g docfx"; exit 1; }
	@export PATH="$$HOME/.dotnet/tools:$$PATH"; docfx docfx.json --serve

check:
	@command -v dotnet >/dev/null 2>&1 || { echo "Error: .NET SDK not found. Install from https://dotnet.microsoft.com/download"; exit 1; }

bench: check
	dotnet run --project Benchmarks/Benchmarks.csproj --configuration Release -- $(if $(FILTER),--filter "*$(FILTER)*",--filter "*")

format: format-style format-analyzers format-whitespace

format-style: check
	@for i in 1 2 3; do \
		dotnet format VoxelGame.sln style --severity info -v quiet; \
		dotnet format VoxelGame.sln style --severity info --verify-no-changes -v quiet >/dev/null 2>&1 && break; \
	done
	@echo "style: formatted"

format-analyzers: check
	@dotnet format VoxelGame.sln analyzers --severity info -v quiet
	@echo "analyzers: formatted"

format-whitespace: check
	@dotnet format VoxelGame.sln whitespace -v quiet
	@echo "whitespace: formatted"

format-check: check
	@dotnet format VoxelGame.sln style --severity info --verify-no-changes
	@dotnet format VoxelGame.sln analyzers --severity info --verify-no-changes
	@dotnet format VoxelGame.sln whitespace --verify-no-changes
	@echo "format-check: clean"
