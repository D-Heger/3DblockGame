.DEFAULT_GOAL := help

.PHONY: help restore build run test clean docs

help:
	@echo "Available commands:"
	@echo "make restore - Restore NuGet packages"
	@echo "make build  - Build the solution"
	@echo "make run    - Run the game"
	@echo "make test   - Run the tests"
	@echo "make clean  - Clean build outputs"
	@echo "make docs   - Generate and serve documentation"

restore:
	dotnet restore VoxelGame.sln

build: restore
	dotnet build VoxelGame.sln --configuration Release

run: build
	dotnet run --project VoxelGame/VoxelGame.csproj

test:
	dotnet test Tests/Tests.csproj

clean:
	dotnet clean VoxelGame.sln
	rm -rf VoxelGame/bin VoxelGame/obj
	rm -rf Tests/bin Tests/obj
	rm -rf Docs

docs:
	naturaldocs -i VoxelGame -i Tests -o Docs -p ProjectConfig
	xdg-open http://localhost:8000 || open http://localhost:8000 || start http://localhost:8000
