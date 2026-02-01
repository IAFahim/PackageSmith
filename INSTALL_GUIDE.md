# PackageSmith Installation Guide

## Quick Start

### Prerequisites
- .NET SDK 9.0 or higher ([Download](https://dotnet.microsoft.com/download))
- Linux, macOS, or Windows with PowerShell

### Linux/macOS Installation

```bash
# Clone or navigate to repository
cd PackageSmith

# Run installer
./install.sh

# Update current shell PATH (choose one):
export PATH="$PATH:$HOME/.local/share/pksmith"  # Current session only
# OR
exec bash  # Restart shell with updated PATH
# OR
source ~/.bashrc  # Reload configuration

# Verify installation
pksmith --help
```

### Windows Installation

```powershell
# Run installer
.\install.ps1

# Verify installation
pksmith --help
```

## Installation Locations

### Linux/macOS
- **Binary**: `~/.local/share/pksmith/pksmith`
- **Config**: `~/.bashrc` or `~/.zshrc`
- **Templates**: `~/.local/share/PackageSmith/Templates/`
- **PATH Addition**: `export PATH="$PATH:$HOME/.local/share/pksmith"`

### Windows
- **Binary**: `%LOCALAPPDATA%\PackageSmith\pksmith.exe`
- **Templates**: `%LOCALAPPDATA%\PackageSmith\Templates\`
- **PATH**: Added to user environment variables

## Troubleshooting

### "pksmith: command not found" (Linux/macOS)

**Cause**: Current shell doesn't have updated PATH

**Solution**:
```bash
# Option 1: Update current session
export PATH="$PATH:$HOME/.local/share/pksmith"

# Option 2: Restart shell
exec bash

# Option 3: Source config
source ~/.bashrc

# Option 4: Use full path
~/.local/share/pksmith/pksmith --help
```

### "dotnet: command not found"

**Cause**: .NET SDK not installed or not in PATH

**Solution**:
```bash
# Install .NET SDK
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0

# Add to PATH
export PATH="$PATH:$HOME/.dotnet"
```

### "Cannot write to /usr/local/bin"

**Cause**: Normal - installer uses local directory instead

**Solution**: No action needed. The installer automatically:
1. Installs to `~/.local/share/pksmith/`
2. Adds to PATH via shell config
3. Works for current user without sudo

### Running with sudo fails (.NET not found)

**Cause**: sudo runs in different environment without .NET

**Solution**: Don't use sudo. The installer works without it:
```bash
./install.sh  # No sudo needed
```

### Build Errors

**"PackageSmith.sln not found"**
```bash
# Make sure you're in the repository root
cd PackageSmith
ls PackageSmith.sln  # Should exist
```

**"Build failed"**
```bash
# Check .NET version
dotnet --version  # Should be 9.0 or higher

# Clean and rebuild
dotnet clean
dotnet build -c Release
```

## Uninstallation

### Linux/macOS
```bash
./uninstall.sh
```

Or manually:
```bash
# Remove binary
rm -rf ~/.local/share/pksmith

# Remove from PATH
sed -i '/PackageSmith/d' ~/.bashrc
```

### Windows
```powershell
.\uninstall.ps1
```

## Verification

After installation, verify with:

```bash
# Check command exists
which pksmith  # Should show path

# Check version
pksmith --help  # Shows commands

# List templates
pksmith templates
```

## Manual Installation (Alternative)

If the installer doesn't work:

```bash
# Build
dotnet build -c Release

# Copy binary
mkdir -p ~/.local/share/pksmith
cp src/PackageSmith.App/bin/Release/net9.0/* ~/.local/share/pksmith/
mv ~/.local/share/pksmith/PackageSmith.App ~/.local/share/pksmith/pksmith
chmod +x ~/.local/share/pksmith/pksmith

# Add to PATH manually
echo 'export PATH="$PATH:$HOME/.local/share/pksmith"' >> ~/.bashrc
source ~/.bashrc
```

## System-Wide Installation (Linux/macOS)

For system-wide access (all users):

```bash
# Build
dotnet build -c Release

# Install (requires sudo)
sudo mkdir -p /usr/local/share/pksmith
sudo cp src/PackageSmith.App/bin/Release/net9.0/* /usr/local/share/pksmith/
sudo mv /usr/local/share/pksmith/PackageSmith.App /usr/local/share/pksmith/pksmith
sudo chmod +x /usr/local/share/pksmith/pksmith
sudo ln -sf /usr/local/share/pksmith/pksmith /usr/local/bin/pksmith

# Verify
pksmith --help
```

## Docker Installation

For containerized environments:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0

WORKDIR /app
COPY . .

RUN dotnet build -c Release
RUN mkdir -p /usr/local/share/pksmith && \
    cp src/PackageSmith.App/bin/Release/net9.0/* /usr/local/share/pksmith/ && \
    mv /usr/local/share/pksmith/PackageSmith.App /usr/local/share/pksmith/pksmith && \
    chmod +x /usr/local/share/pksmith/pksmith && \
    ln -s /usr/local/share/pksmith/pksmith /usr/local/bin/pksmith

CMD ["pksmith", "--help"]
```

## CI/CD Installation

For GitHub Actions:

```yaml
- name: Install .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '9.0.x'

- name: Install PackageSmith
  run: |
    git clone https://github.com/IAFahim/PackageSmith.git
    cd PackageSmith
    ./install.sh
    export PATH="$PATH:$HOME/.local/share/pksmith"

- name: Use PackageSmith
  run: pksmith --help
```

## Next Steps

After successful installation:

1. **Explore templates**:
   ```bash
   pksmith templates
   ```

2. **Create your first package**:
   ```bash
   pksmith new com.yourcompany.utilities --template ecs
   ```

3. **Generate CI workflows**:
   ```bash
   pksmith ci generate -o ./your-package
   ```

## Support

If issues persist:

1. Check `.NET SDK` version: `dotnet --version`
2. Check installation location: `ls -la ~/.local/share/pksmith/`
3. Check PATH: `echo $PATH | grep pksmith`
4. Test direct execution: `~/.local/share/pksmith/pksmith --help`
5. Check shell config: `tail ~/.bashrc`

Still having issues? File an issue with:
- OS and version
- .NET SDK version (`dotnet --version`)
- Full error output
- Installation method used
