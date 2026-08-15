.PHONY: run build test clean docs check

run: build
	dotnet run --project VoxelGame/VoxelGame.csproj

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
