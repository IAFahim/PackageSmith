#!/bin/bash

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
NC='\033[0m'

MIN_DOTNET_VERSION="8.0.0"

echo -e "${CYAN}"
echo "========================================"
echo "  iupk Installer for Linux/Mac"
echo "========================================"
echo -e "${NC}"

check_dotnet() {
    if ! command -v dotnet &> /dev/null; then
        echo -e "${YELLOW}.NET SDK not found${NC}"
        echo -e "${WHITE}Install from: https://dotnet.microsoft.com/download/dotnet/8.0${NC}"
        exit 1
    fi

    local version=$(dotnet --version 2>&1)
    echo -e "${GREEN}Found .NET $version${NC}"
}

build_solution() {
    echo -e "${CYAN}Building PackageSmith...${NC}"

    local script_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    local sln_path="$script_root/PackageSmith.sln"

    if [ ! -f "$sln_path" ]; then
        echo -e "${RED}Error: PackageSmith.sln not found at: $sln_path${NC}"
        exit 1
    fi

    dotnet build "$sln_path" -c Release

    if [ $? -ne 0 ]; then
        echo -e "${RED}Build failed!${NC}"
        exit 1
    fi

    echo -e "${GREEN}Build successful!${NC}"
}

install_binary() {
    echo -e "${CYAN}Installing iupk...${NC}"

    local script_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    local bin_dir="$script_root/src/PackageSmith/bin/Release/net8.0"
    local install_dir="$HOME/.local/share/iupk"
    local bin_install="$install_dir/iupk"

    if [ ! -f "$bin_dir/PackageSmith" ]; then
        echo -e "${RED}Error: Built binary not found at: $bin_dir/PackageSmith${NC}"
        exit 1
    fi

    mkdir -p "$install_dir"
    cp "$bin_dir/PackageSmith" "$bin_install"
    chmod +x "$bin_install"

    if [ -w /usr/local/bin ]; then
        ln -sf "$bin_install" /usr/local/bin/iupk
        echo -e "${GREEN}Symlinked to /usr/local/bin/iupk${NC}"
    else
        echo -e "${YELLOW}Cannot write to /usr/local/bin (requires sudo)${NC}"
        echo -e "${CYAN}Adding to PATH via shell config...${NC}"

        detect_shell_and_add_path "$install_dir"
    fi

    echo -e "${GREEN}Installed to: $bin_install${NC}"
}

detect_shell_and_add_path() {
    local install_dir="$1"

    if [ -n "$ZSH_VERSION" ]; then
        local shell_config="$HOME/.zshrc"
    elif [ -n "$BASH_VERSION" ]; then
        local shell_config="$HOME/.bashrc"
    else
        local shell_config="$HOME/.profile"
    fi

    local path_line="export PATH=\"\$PATH:$install_dir\""

    if ! grep -q "$install_dir" "$shell_config" 2>/dev/null; then
        echo "" >> "$shell_config"
        echo "# iupk" >> "$shell_config"
        echo "$path_line" >> "$shell_config"
        echo -e "${GREEN}Added to $shell_config${NC}"
        echo -e "${YELLOW}Run 'source $shell_config' or restart your shell${NC}"
    else
        echo -e "${GREEN}PATH already configured in $shell_config${NC}"
    fi
}

test_installation() {
    echo -e "${CYAN}Testing installation...${NC}"

    if command -v iupk &> /dev/null; then
        local version=$(iupk --version 2>&1 || echo "ok")
        echo -e "${GREEN}Installation successful!${NC}"
        echo -e "${CYAN}Run 'iupk' from any directory to use.${NC}"
        return 0
    else
        echo -e "${YELLOW}iupk command not found in PATH${NC}"
        echo -e "${YELLOW}You may need to restart your shell${NC}"
        return 1
    fi
}

check_dotnet
build_solution
install_binary
test_installation

echo -e "${CYAN}"
echo "========================================"
echo -e "  ${GREEN}Installation Complete!${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""
