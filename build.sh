#!/bin/bash

# =========================================================
# 3D Block Game Build Script for Linux
# =========================================================
# This script manages the build process for the 3D Block Game
# project on Linux systems. It handles package restoration,
# building, testing, and documentation generation.
#
# Exit Codes:
#   0 - Success
#   1 - Missing requirements or tool failure
#   >1 - Command-specific error
# =========================================================

# Store the script's exit code
EXIT_CODE=0

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to detect Linux distribution
get_distro() {
    if [ -f /etc/os-release ]; then
        . /etc/os-release
        echo $ID
    else
        echo "unknown"
    fi
}

# Function to install packages based on distribution
install_package() {
    local package=$1
    local distro=$(get_distro)
    
    case $distro in
        "ubuntu"|"debian")
            sudo apt-get update && sudo apt-get install -y $package
            ;;
        "fedora")
            sudo dnf install -y $package
            ;;
        "arch")
            sudo pacman -Sy --noconfirm $package
            ;;
        *)
            echo "Unsupported distribution for automatic installation"
            return 1
            ;;
    esac
}

# Function to check system requirements
check_requirements() {
    echo "Checking system requirements..."
    
    # Check for .NET SDK
    if ! command_exists dotnet; then
        echo "Error: .NET SDK is not installed or not in PATH"
        read -p "Would you like to install .NET SDK? [y/N] " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            distro=$(get_distro)
            case $distro in
                "ubuntu"|"debian")
                    wget https://packages.microsoft.com/config/$distro/$(lsb_release -rs)/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
                    sudo dpkg -i /tmp/packages-microsoft-prod.deb
                    sudo apt-get update
                    sudo apt-get install -y dotnet-sdk-8.0
                    rm /tmp/packages-microsoft-prod.deb
                    ;;
                "fedora")
                    sudo dnf install dotnet-sdk-8.0
                    ;;
                "arch")
                    sudo pacman -Sy --noconfirm dotnet-sdk
                    ;;
                *)
                    echo "Please install .NET SDK manually from https://dotnet.microsoft.com/download"
                    return 1
                    ;;
            esac
        else
            echo "Please install .NET SDK manually from https://dotnet.microsoft.com/download"
            return 1
        fi
    fi

    # Check .NET SDK version
    DOTNET_VERSION=$(dotnet --version)
    echo "Found .NET SDK version $DOTNET_VERSION"
    echo "--------------------------------------------"

    # Check for OpenGL dependencies (mesa-utils)
    if ! command_exists glxinfo; then
        echo "Warning: mesa-utils is not installed (needed for OpenGL support)"
        read -p "Would you like to install mesa-utils? [y/N] " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            distro=$(get_distro)
            case $distro in
                "ubuntu"|"debian")
                    sudo apt-get update && sudo apt-get install -y mesa-utils
                    ;;
                "fedora")
                    sudo dnf install -y glx-utils
                    ;;
                "arch")
                    sudo pacman -Sy --noconfirm mesa-utils
                    ;;
                *)
                    echo "Please install mesa-utils manually using your package manager"
                    ;;
            esac
        fi
    fi
    echo "--------------------------------------------"

    # Check for OpenGL support using glxinfo
    if ! command_exists glxinfo; then
        echo "Warning: glxinfo not found. Please install mesa-utils package to check OpenGL support."
        echo "Try: sudo apt install mesa-utils"
    else
        echo "Checking OpenGL support..."
        RENDERER=$(glxinfo | grep "OpenGL renderer")
        VERSION=$(glxinfo | grep "OpenGL version")
        echo "GPU: $RENDERER"
        echo "OpenGL: $VERSION"
    fi
    echo "--------------------------------------------"

    # Check for docfx
    if ! command_exists docfx; then
        echo "Warning: DocFX is not installed."
        read -p "Would you like to install DocFX? [y/N] " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            echo "Installing DocFX..."
            dotnet tool install -g docfx
            # Add .NET tools to PATH if not already done
            if ! grep -q '.dotnet/tools' "$HOME/.bashrc"; then
                echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> "$HOME/.bashrc"
                # Also add to current session
                export PATH="$PATH:$HOME/.dotnet/tools"
            fi
            echo "DocFX installation complete. Please restart your terminal for PATH changes to take effect."
        else
            echo "You can install DocFX later using: dotnet tool install -g docfx"
        fi
    fi
    echo "--------------------------------------------"

    # Check for Visual Studio Code
    if ! command_exists code; then
        echo "Visual Studio Code is not installed"
        read -p "Would you like to install VS Code? [y/N] " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            distro=$(get_distro)
            case $distro in
                "ubuntu"|"debian")
                    echo "Installing VS Code for Ubuntu/Debian..."
                    sudo apt-get install -y wget gpg apt-transport-https
                    wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > /tmp/packages.microsoft.gpg
                    sudo install -D -o root -g root -m 644 /tmp/packages.microsoft.gpg /etc/apt/keyrings/packages.microsoft.gpg
                    sudo sh -c 'echo "deb [arch=amd64,arm64,armhf signed-by=/etc/apt/keyrings/packages.microsoft.gpg] https://packages.microsoft.com/repos/code stable main" > /etc/apt/sources.list.d/vscode.list'
                    rm -f /tmp/packages.microsoft.gpg
                    sudo apt-get update
                    sudo apt-get install -y code
                    ;;
                "fedora")
                    echo "Installing VS Code for Fedora..."
                    sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
                    sudo sh -c 'echo -e "[code]\nname=Visual Studio Code\nbaseurl=https://packages.microsoft.com/yumrepos/vscode\nenabled=1\ngpgcheck=1\ngpgkey=https://packages.microsoft.com/keys/microsoft.asc" > /etc/yum.repos.d/vscode.repo'
                    sudo dnf install -y code
                    ;;
                "arch")
                    echo "Installing VS Code for Arch Linux..."
                    sudo pacman -Sy --noconfirm code
                    ;;
                *)
                    echo "Please install Visual Studio Code manually from https://code.visualstudio.com/"
                    ;;
            esac
            echo "VS Code installation complete."
        else
            echo "You can install VS Code later from https://code.visualstudio.com/"
        fi
    fi
    echo "--------------------------------------------"

    return 0
}

# Function to restore packages
restore() {
    check_requirements || return $?
    echo "Restoring NuGet packages..."
    dotnet restore VoxelGame.sln
    return $?
}

# Function to build the solution
build() {
    restore || return $?
    echo "Building solution..."
    dotnet build VoxelGame.sln --configuration Release
    return $?
}

# Function to run the game
run() {
    build || return $?
    echo "Running the game..."
    dotnet run --project VoxelGame/VoxelGame.csproj
    return $?
}

# Function to run tests
test() {
    check_requirements || return $?
    echo "Running tests..."
    dotnet test Tests/Tests.csproj
    return $?
}

# Function to clean build outputs
clean() {
    echo "Cleaning solution..."
    dotnet clean VoxelGame.sln
    
    # Clean additional directories
    rm -rf VoxelGame/bin VoxelGame/obj Tests/bin Tests/obj
    return $?
}

# Function to generate documentation
docs() {
    check_requirements || return $?
    if ! command_exists docfx; then
        echo "Error: DocFX is not installed. Please install it using: dotnet tool install -g docfx"
        return 1
    fi
    echo "Generating documentation..."
    docfx docfx.json --serve
    return $?
}

# Function to display help
show_help() {
    echo "========================================================="
    echo "3D Block Game Build Script"
    echo "========================================================="
    echo "Available commands:"
    echo "./build.sh restore - Restore NuGet packages"
    echo "./build.sh build   - Build the solution"
    echo "./build.sh run     - Run the game"
    echo "./build.sh test    - Run the tests"
    echo "./build.sh clean   - Clean build outputs"
    echo "./build.sh docs    - Generate documentation"
    echo "./build.sh check   - Check system requirements"
    echo "========================================================="
}

# Main script logic
case "$1" in
    "restore")
        restore
        EXIT_CODE=$?
        ;;
    "build")
        build
        EXIT_CODE=$?
        ;;
    "run")
        run
        EXIT_CODE=$?
        ;;
    "test")
        test
        EXIT_CODE=$?
        ;;
    "clean")
        clean
        EXIT_CODE=$?
        ;;
    "docs")
        docs
        EXIT_CODE=$?
        ;;
    "check")
        check_requirements
        EXIT_CODE=$?
        ;;
    *)
        show_help
        ;;
esac

exit $EXIT_CODE