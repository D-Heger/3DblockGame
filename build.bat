@echo off
SETLOCAL EnableDelayedExpansion

REM Store the script's exit code
SET EXIT_CODE=0

IF "%1"=="" GOTO help

IF "%1"=="restore" GOTO restore
IF "%1"=="build" GOTO build
IF "%1"=="run" GOTO run
IF "%1"=="test" GOTO test
IF "%1"=="clean" GOTO clean
IF "%1"=="docs" GOTO docs
IF "%1"=="check" GOTO check

:help
echo Available commands:
echo build.bat restore - Restore NuGet packages
echo build.bat build  - Build the solution
echo build.bat run    - Run the game
echo build.bat test   - Run the tests
echo build.bat clean  - Clean build outputs
echo build.bat docs   - Generate documentation
echo build.bat check  - Check system requirements
GOTO end

:check
CALL :check_requirements
IF !EXIT_CODE! NEQ 0 GOTO end
echo All requirements are satisfied.
GOTO end

:check_requirements
echo Checking system requirements...

REM Check for .NET SDK
dotnet --version > nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo Error: .NET SDK is not installed or not in PATH
    echo Please install .NET SDK from https://dotnet.microsoft.com/download
    SET EXIT_CODE=1
    exit /b 1
)

REM Check .NET SDK version
for /f "tokens=*" %%a in ('dotnet --version') do set DOTNET_VERSION=%%a
echo Found .NET SDK version !DOTNET_VERSION!
echo --------------------------------------------

REM Check for Visual Studio (optional)
reg query "HKLM\SOFTWARE\Microsoft\VisualStudio\15.0" > nul 2>&1 || ^
reg query "HKLM\SOFTWARE\Microsoft\VisualStudio\16.0" > nul 2>&1 || ^
reg query "HKLM\SOFTWARE\Microsoft\VisualStudio\17.0" > nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo Warning: Visual Studio 2017 or later not found. It's recommended, as it makes the setup easier, but not required.
    echo          I personally use Visual Studio Code with the C# extension. You do you.
) ELSE (
    echo Visual Studio detected. Setup will proceed as normal.
)
echo --------------------------------------------

REM Check for OpenGL support using PowerShell
echo Checking OpenGL support...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-WmiObject Win32_VideoController | ForEach-Object { Write-Output \"GPU:            $($_.Name)`nDriver Version: $($_.DriverVersion)\" }"
IF !ERRORLEVEL! NEQ 0 (
    echo Warning: Could not verify GPU capabilities. Please ensure your graphics drivers are up to date.
) ELSE (
    echo GPU check passed. OpenGL support should be available.
)
echo --------------------------------------------

REM Check if docfx is installed (for documentation)
where docfx > nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo Warning: DocFX is not installed. Documentation generation will not be available.
    echo Install DocFX using: dotnet tool install -g docfx
) ELSE (
    echo DocFX is installed. Documentation generation will be available.
)
echo --------------------------------------------

exit /b 0

:restore
CALL :check_requirements
IF !EXIT_CODE! NEQ 0 GOTO end
echo Restoring NuGet packages...
dotnet restore VoxelGame.sln
IF !ERRORLEVEL! NEQ 0 SET EXIT_CODE=!ERRORLEVEL!
GOTO end

:build
CALL :restore
IF !EXIT_CODE! NEQ 0 GOTO end
echo Building solution...
dotnet build VoxelGame.sln --configuration Release
IF !ERRORLEVEL! NEQ 0 SET EXIT_CODE=!ERRORLEVEL!
GOTO end

:run
CALL :build
IF !EXIT_CODE! NEQ 0 GOTO end
echo Running the game...
dotnet run --project VoxelGame/VoxelGame.csproj
IF !ERRORLEVEL! NEQ 0 SET EXIT_CODE=!ERRORLEVEL!
GOTO end

:test
CALL :check_requirements
IF !EXIT_CODE! NEQ 0 GOTO end
echo Running tests...
dotnet test Tests/Tests.csproj
IF !ERRORLEVEL! NEQ 0 SET EXIT_CODE=!ERRORLEVEL!
GOTO end

:clean
echo Cleaning solution...
dotnet clean VoxelGame.sln
IF EXIST VoxelGame\bin rmdir /s /q VoxelGame\bin
IF EXIST VoxelGame\obj rmdir /s /q VoxelGame\obj
IF EXIST Tests\bin rmdir /s /q Tests\bin
IF EXIST Tests\obj rmdir /s /q Tests\obj
IF !ERRORLEVEL! NEQ 0 SET EXIT_CODE=!ERRORLEVEL!
GOTO end

:docs
CALL :check_requirements
IF !EXIT_CODE! NEQ 0 GOTO end
where docfx > nul 2>&1
IF !ERRORLEVEL! NEQ 0 (
    echo Error: DocFX is not installed. Please install it using: dotnet tool install -g docfx
    SET EXIT_CODE=1
    GOTO end
)
echo Generating documentation...
docfx docfx.json --serve
IF !ERRORLEVEL! NEQ 0 SET EXIT_CODE=!ERRORLEVEL!
GOTO end

:end
EXIT /B !EXIT_CODE!