@echo off
SETLOCAL EnableDelayedExpansion

REM =========================================================
REM 3D Block Game Build Script
REM =========================================================
REM This script manages the build process for the 3D Block Game
REM project. It handles package restoration, building, testing,
REM and documentation generation.
REM
REM Exit Codes:
REM   0 - Success
REM   1 - Missing requirements or tool failure
REM   >1 - Command-specific error
REM =========================================================

REM Store the script's exit code
SET EXIT_CODE=0

IF "%1"=="" GOTO help

REM Command routing
IF "%1"=="restore" GOTO restore
IF "%1"=="build" GOTO build
IF "%1"=="run" GOTO run
IF "%1"=="test" GOTO test
IF "%1"=="clean" GOTO clean
IF "%1"=="docs" GOTO docs
IF "%1"=="check" GOTO check

:help
echo =========================================================
echo 3D Block Game Build Script
echo =========================================================
echo Available commands:
echo build.bat restore  - Restore NuGet packages
echo build.bat build    - Build the solution
echo build.bat run      - Run the game
echo build.bat test     - Run the tests
echo build.bat clean    - Clean build outputs
echo build.bat docs     - Generate documentation
echo build.bat check    - Check system requirements
echo =========================================================
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
    echo Would you like to download and install .NET SDK? [Y/N]
    SET /P INSTALL_DOTNET=
    IF /I "!INSTALL_DOTNET!"=="Y" (
        echo Downloading .NET SDK installer...
        powershell -Command "& { Invoke-WebRequest -Uri 'https://download.visualstudio.microsoft.com/download/pr/bd44cdb8-dcac-4f1f-8246-1ee392c68dac/ba818a6e513c305d4438c7da45c2b085/dotnet-sdk-8.0.406-win-x64.exe' -OutFile '%TEMP%\dotnet-sdk-installer.exe' }"
        echo Installing .NET SDK...
        start /wait %TEMP%\dotnet-sdk-installer.exe /quiet
        del %TEMP%\dotnet-sdk-installer.exe
        echo Please restart this script after installation completes.
        SET EXIT_CODE=1
        exit /b 1
    ) ELSE (
        echo Please install .NET SDK manually from https://dotnet.microsoft.com/download
        SET EXIT_CODE=1
        exit /b 1
    )
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
    echo Warning: DocFX is not installed.
    echo Would you like to install DocFX? [Y/N]
    SET /P INSTALL_DOCFX=
    IF /I "!INSTALL_DOCFX!"=="Y" (
        echo Installing DocFX...
        dotnet tool install -g docfx
        echo DocFX installation complete.
    ) ELSE (
        echo You can install DocFX later using: dotnet tool install -g docfx
    )
) ELSE (
    echo DocFX is installed.
)
echo --------------------------------------------

REM Check for Visual Studio Code (recommended alternative to VS)
where code > nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo Would you like to install Visual Studio Code? [Y/N]
    SET /P INSTALL_VSCODE=
    IF /I "!INSTALL_VSCODE!"=="Y" (
        echo Downloading Visual Studio Code installer...
        powershell -Command "& { Invoke-WebRequest -Uri 'https://code.visualstudio.com/sha/download?build=stable&os=win32-x64-user' -OutFile '%TEMP%\vscode-installer.exe' }"
        echo Installing Visual Studio Code...
        start /wait %TEMP%\vscode-installer.exe /SILENT /NORESTART
        del %TEMP%\vscode-installer.exe
        echo Please restart this script after installation completes.
    ) ELSE (
        echo You can download VS Code later from https://code.visualstudio.com/
    )
)

exit /b 0

:restore
REM Ensure all requirements are met before proceeding
CALL :check_requirements
IF !EXIT_CODE! NEQ 0 GOTO end
echo Restoring NuGet packages...
dotnet restore VoxelGame.sln
IF !ERRORLEVEL! NEQ 0 SET EXIT_CODE=!ERRORLEVEL!
GOTO end

:build
REM Build depends on successful package restoration
CALL :restore
IF !EXIT_CODE! NEQ 0 GOTO end
echo Building solution...
dotnet build VoxelGame.sln --configuration Release
IF !ERRORLEVEL! NEQ 0 SET EXIT_CODE=!ERRORLEVEL!
GOTO end

:run
REM Run depends on successful build
CALL :build
IF !EXIT_CODE! NEQ 0 GOTO end
echo Running the game...
dotnet run --project VoxelGame/VoxelGame.csproj
IF !ERRORLEVEL! NEQ 0 SET EXIT_CODE=!ERRORLEVEL!
GOTO end

:test
REM Tests require build requirements but don't need a full build
CALL :check_requirements
IF !EXIT_CODE! NEQ 0 GOTO end
echo Running tests...
dotnet test Tests/Tests.csproj
IF !ERRORLEVEL! NEQ 0 SET EXIT_CODE=!ERRORLEVEL!
GOTO end

:clean
echo Cleaning solution...
dotnet clean VoxelGame.sln
REM Clean additional directories that might not be caught by dotnet clean
IF EXIST VoxelGame\bin rmdir /s /q VoxelGame\bin
IF EXIST VoxelGame\obj rmdir /s /q VoxelGame\obj
IF EXIST Tests\bin rmdir /s /q Tests\bin
IF EXIST Tests\obj rmdir /s /q Tests\obj
IF !ERRORLEVEL! NEQ 0 SET EXIT_CODE=!ERRORLEVEL!
GOTO end

:docs
REM Documentation generation requires DocFX
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