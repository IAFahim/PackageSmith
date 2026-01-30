#!/bin/bash

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${CYAN}"
echo "========================================"
echo "  iupk Uninstaller for Linux/Mac"
echo "========================================"
echo -e "${NC}"

INSTALL_DIR="$HOME/.local/share/iupk"
SYMLINK="/usr/local/bin/iupk"

if [ ! -d "$INSTALL_DIR" ] && [ ! -L "$SYMLINK" ]; then
    echo -e "${YELLOW}iupk is not installed.${NC}"
    exit 0
fi

echo -e "${CYAN}Removing iupk...${NC}"

if [ -L "$SYMLINK" ]; then
    if [ -w /usr/local/bin ]; then
        rm "$SYMLINK"
        echo -e "${GREEN}Removed symlink: $SYMLINK${NC}"
    else
        echo -e "${YELLOW}Cannot remove $SYMLINK (requires sudo)${NC}"
        echo -e "${CYAN}Run: sudo rm $SYMLINK${NC}"
    fi
fi

if [ -d "$INSTALL_DIR" ]; then
    rm -rf "$INSTALL_DIR"
    echo -e "${GREEN}Removed: $INSTALL_DIR${NC}"
fi

detect_shell_and_remove_path() {
    local install_dir="$1"

    if [ -n "$ZSH_VERSION" ]; then
        local shell_config="$HOME/.zshrc"
    elif [ -n "$BASH_VERSION" ]; then
        local shell_config="$HOME/.bashrc"
    else
        local shell_config="$HOME/.profile"
    fi

    if grep -q "$install_dir" "$shell_config" 2>/dev/null; then
        sed -i.tmp "/$install_dir/d" "$shell_config"
        rm -f "${shell_config}.tmp"
        echo -e "${GREEN}Removed from $shell_config${NC}"
        echo -e "${YELLOW}Run 'source $shell_config' or restart your shell${NC}"
    fi
}

detect_shell_and_remove_path "$INSTALL_DIR"

echo -e "${CYAN}"
echo "========================================"
echo -e "  ${GREEN}Uninstallation Complete!${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""
