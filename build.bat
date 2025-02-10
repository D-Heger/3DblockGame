@echo off
IF "%1"=="" GOTO help

IF "%1"=="restore" GOTO restore
IF "%1"=="build" GOTO build
IF "%1"=="run" GOTO run
IF "%1"=="test" GOTO test
IF "%1"=="clean" GOTO clean
IF "%1"=="docs" GOTO docs

:help
echo Available commands:
echo build.bat restore - Restore NuGet packages
echo build.bat build  - Build the solution
echo build.bat run    - Run the game
echo build.bat test   - Run the tests
echo build.bat clean  - Clean build outputs
echo build.bat docs   - Generate and serve documentation
GOTO :eof

:restore
dotnet restore VoxelGame.sln
GOTO :eof

:build
CALL :restore
dotnet build VoxelGame.sln --configuration Release
GOTO :eof

:run
CALL :build
dotnet run --project VoxelGame/VoxelGame.csproj
GOTO :eof

:test
dotnet test Tests/Tests.csproj
GOTO :eof

:clean
dotnet clean VoxelGame.sln
IF EXIST VoxelGame\bin rmdir /s /q VoxelGame\bin
IF EXIST VoxelGame\obj rmdir /s /q VoxelGame\obj
IF EXIST Tests\bin rmdir /s /q Tests\bin
IF EXIST Tests\obj rmdir /s /q Tests\obj
IF EXIST Docs rmdir /s /q Docs
GOTO :eof

:docs
naturaldocs -i VoxelGame -i Tests -o Docs -p ProjectConfig
start "" "http://localhost:8000"
GOTO :eof
