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

### Validation

```bash
# Validate current directory
pksmith validate

# Validate specific path
pksmith validate ./MyPackage

# Verbose output
pksmith validate ./MyPackage --verbose
```

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
