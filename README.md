# PackageSmith

> **Build Unity packages the right way, every time.**

PackageSmith is a professional CLI tool for scaffolding and managing Unity packages with sophisticated architectures. Generate modular ECS structures, manage assembly dependencies, and install packages—all from your terminal.

```bash
# Create a new package with ECS modular architecture
pksmith new ecs-modular

# Interactive package creation
pksmith new --interactive

# Install package to Unity project
pksmith install ./MyPackage

# List available templates
pksmith templates list
```

## Features

✨ **Sophisticated Templates** - Generate complex modular architectures (Data/Authoring/Runtime/Systems/Editor/Debug)  
🎯 **Assembly Management** - Automatic dependency resolution and InternalsVisibleTo configuration  
🚀 **Interactive CLI** - Rich terminal experience with live previews and validation  
📦 **Package Installation** - Seamlessly install local packages to Unity projects  
🔍 **Validation** - Check package structure against Unity best practices  
⚙️ **Extensible** - Create custom templates for your team's needs  

## Installation

### Prerequisites

- .NET SDK 8.0 or higher
- Unity 2022.3+ (for package installation features)

### Quick Install

**Windows (PowerShell)**
```powershell
.\install.ps1
```

**Linux/Mac (Bash/Zsh)**
```bash
chmod +x install.sh
./install.sh
```

After installation, restart your terminal and run `pksmith` to verify.

## Usage

### Creating Packages

**Interactive mode** (recommended for first-time users):
```bash
pksmith new --interactive
```

**From template** (faster for experienced users):
```bash
# Basic Unity package
pksmith new basic

# Simple ECS structure
pksmith new ecs-simple

# Advanced modular ECS with sub-assemblies
pksmith new ecs-modular
```

**With options**:
```bash
pksmith new ecs-modular \
  --name com.mycompany.coolfeature \
  --namespace MyCompany.CoolFeature \
  --author "My Studio" \
  --unity-version 2022.3
```

### Template Discovery

```bash
# List all available templates
pksmith templates list

# Show template details
pksmith templates info ecs-modular

# Show what files a template will generate
pksmith templates preview ecs-modular
```

### Package Management

```bash
# Install from current directory
pksmith install

# Install from specific path
pksmith install ../MyPackage

# Install to specific Unity project
pksmith install ./MyPackage -p /path/to/unity/project

# List installed packages
pksmith list

# Remove a package
pksmith remove com.example.mypackage
```

### Package Transfer (Development Workflow)

Move packages between Library and Packages folders for easy testing and editing:

```bash
# Auto-detect and transfer
pksmith transfer com.company.package

# Move to Packages for editing (from Library)
pksmith transfer com.company.package --to-packages

# Move to Library for testing as installed (from Packages)
pksmith transfer com.company.package --to-library

# Transfer without backup
pksmith transfer com.company.package --no-backup

# Force transfer without confirmation
pksmith transfer com.company.package --force
```

**Use Cases**:
- **To Packages** (`--to-packages`): Move package from Library/PackageCache to Packages/ to make it editable for fixing bugs or adding features
- **To Library** (`--to-library`): Move package from Packages/ to test it as if installed from registry

**What it does**:
- ✅ Automatically creates backups (unless `--no-backup`)
- ✅ Updates `manifest.json` with appropriate references
- ✅ Preserves package contents safely (including .git folders)
- ✅ Auto-detects transfer direction if not specified

### Git Integration (Push Fixes Directly)

Link packages to git repositories for seamless development workflow:

```bash
# Link package to git repository
pksmith git link com.company.package https://github.com/user/repo.git

# Clone package repository directly to Packages/
pksmith git clone https://github.com/user/com.company.package.git

# Check git status
pksmith git status com.company.package

# Push your fixes
pksmith git push com.company.package

# Pull latest changes
pksmith git pull com.company.package

# Unlink git repository
pksmith git unlink com.company.package
```

**Complete Workflow**:
```bash
# 1. Transfer installed package to Packages/ for editing
pksmith transfer com.unity.entities --to-packages

# 2. Link to your fork
pksmith git link com.unity.entities https://github.com/yourname/entities.git

# 3. Make your fixes in Packages/com.unity.entities/

# 4. Commit and push
cd Packages/com.unity.entities
git add .
git commit -m "Fix: Fixed that annoying bug"
pksmith git push com.unity.entities

# Done! No manual git cloning or folder juggling needed
```

**Features**:
- ✅ Auto-transfers packages from Library if needed
- ✅ Preserves .git during transfers  
- ✅ Clone repos directly to Packages/
- ✅ Push/pull without leaving CLI
- ✅ Check status across multiple packages

### Validation

```bash
# Validate current directory
pksmith validate

# Validate specific path
pksmith validate ./MyPackage

# Verbose output
pksmith validate ./MyPackage --verbose
```

### CI/CD Workflow Generation

Generate GitHub Actions workflows using GameCI for comprehensive package testing:

```bash
# Generate test workflow (simple)
pksmith ci generate --simple

# Generate full workflows (test + build for all platforms)
pksmith ci generate

# Specify Unity versions
pksmith ci generate --unity-versions 2021.3,2022.3,2023.2

# Specify platforms to build
pksmith ci generate --platforms StandaloneWindows64,Android,iOS,WebGL

# Show setup instructions
pksmith ci add-secrets
```

**What it generates**:
- `test.yml` - Run tests on multiple Unity versions
- `build.yml` - Build for multiple platforms (Windows/Mac/Linux/Android/iOS/WebGL)
- `activation.yml` - Unity license activation helper

**Features**:
- ✅ Tests package installation in fresh Unity project
- ✅ Runs tests in EditMode and PlayMode
- ✅ Builds for all platforms to verify compilation
- ✅ Matrix builds (multiple Unity versions × multiple platforms)
- ✅ Caches Unity Library for faster builds
- ✅ Uses GameCI actions (industry standard)

### Configuration

```bash
# View current settings
pksmith config list

# Set default author
pksmith config set author "Your Name"

# Set default Unity version
pksmith config set unity-version "2022.3"

# Get specific setting
pksmith config get author
```

## Template Types

PackageSmith includes several built-in templates:

| Template | Description | Use Case |
|----------|-------------|----------|
| `basic` | Simple Runtime/Editor structure | General-purpose packages |
| `ecs-simple` | Single assembly ECS package | Small ECS features |
| `ecs-modular` | Multi-assembly ECS (Data/Authoring/Runtime/Systems/Editor/Debug) | Production ECS packages |
| `tool` | Editor-only tool package | Unity Editor extensions |
| `shader` | Shader package with examples | Custom shaders/materials |

### ECS Modular Architecture

The `ecs-modular` template generates a sophisticated structure:

```
MyPackage/
├── MyPackage.Data/           # IComponentData, pure data
│   ├── AssemblyInfo.cs       # InternalsVisibleTo configuration
│   └── MyPackage.Data.asmdef
├── MyPackage.Authoring/      # MonoBehaviour + Baker
│   └── MyPackage.Authoring.asmdef (depends on Data)
├── MyPackage/                # Runtime utilities
│   └── MyPackage.asmdef (depends on Data)
├── MyPackage.Systems/        # ISystem implementations
│   └── MyPackage.Systems.asmdef (depends on Data, Runtime)
├── MyPackage.Editor/         # Editor tools
│   └── MyPackage.Editor.asmdef (depends on all)
├── MyPackage.Debug/          # Debug/profiling tools
│   └── MyPackage.Debug.asmdef (depends on Systems)
└── MyPackage.Tests/          # Unit tests
    └── MyPackage.Tests.asmdef
```

**Benefits:**
- Proper separation of concerns
- Faster compilation (only changed assemblies rebuild)
- Clear dependency flow
- InternalsVisibleTo for testing without exposing internals

## Examples

### Create and Install a Package

```bash
# 1. Create package
pksmith new ecs-modular --name com.studio.movement

# 2. Navigate to package
cd com.studio.movement

# 3. Add your code...

# 4. Install to Unity project
pksmith install -p ~/UnityProjects/MyGame
```

### Setup CI/CD for Your Package

```bash
# 1. Navigate to your package
cd com.studio.mypackage

# 2. Generate GitHub Actions workflows
pksmith ci generate

# 3. Commit and push
git add .github/workflows/
git commit -m "Add CI/CD workflows"
git push

# 4. Setup Unity license (one-time)
pksmith ci add-secrets
# Follow the instructions to add GitHub secrets

# 5. Tests and builds will now run automatically on every push!
```

### Development Workflow: Edit Installed Package

```bash
# Package is in Library/PackageCache (read-only)
# Move it to Packages/ to make it editable
pksmith transfer com.unity.entities --to-packages

# Now you can edit it in Packages/com.unity.entities/
# Make your fixes...

# Test it as if installed from registry
pksmith transfer com.unity.entities --to-library
```

### Quickly Fix a Bug in a Package

```bash
# 1. Transfer to Packages for editing
pksmith transfer com.company.buggypackage

# 2. Fix the bug in Packages/com.company.buggypackage/

# 3. Test in Unity

# 4. When done, transfer back or commit changes
pksmith transfer com.company.buggypackage --to-library
```

### Validate Before Committing

```bash
# Run validation in CI/CD or pre-commit hooks
pksmith validate --strict
```

## Development

### Building from Source

```bash
# Clone repository
git clone https://github.com/yourusername/packagesmith
cd packagesmith

# Build
dotnet build PackageSmith.sln -c Release

# Run without installing
dotnet run --project src/PackageSmith -- new --help
```

### Running Tests

```bash
dotnet test
```

## Uninstallation

**Windows**
```powershell
.\uninstall.ps1
```

**Linux/Mac**
```bash
./uninstall.sh
```

## Contributing

Contributions welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Creating Custom Templates

See [Template Authoring Guide](docs/template-authoring.md) for creating your own templates.

## Roadmap

- [ ] Template marketplace
- [ ] Git integration (auto-commit on generation)
- [ ] OpenUPM publishing support
- [ ] Package dependency graph visualization
- [ ] GitHub Actions workflow generation

## License

MIT License - see [LICENSE](LICENSE) for details.

## Credits

Built with ❤️ using [Spectre.Console](https://spectreconsole.net/)

---

**Support**: [Open an issue](https://github.com/yourusername/packagesmith/issues) or join our [Discord](https://discord.gg/example)
