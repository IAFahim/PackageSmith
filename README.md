# PackageSmith

> Unity Package Scaffolding CLI - Create professional Unity packages in seconds.

[![Build](https://github.com/IAFahim/PackageSmith/actions/workflows/test-build.yml/badge.svg)](https://github.com/IAFahim/PackageSmith/actions/workflows/test-build.yml)

PackageSmith is a CLI tool that generates Unity package scaffolding with proper assembly definitions, version defines, CI/CD workflows, and more. Focus on writing your package logic, not boilerplate.

## Features

- **Quick Package Generation** - Create Unity packages from templates or scratch
- **Template System** - Harvest existing packages as reusable templates
- **C# Identifier Sanitization** - Package names with hyphens convert to valid PascalCase (`com.my-tool` → `MyTool`)
- **Automatic versionDefines** - Unity package references get proper version defines
- **Git Integration** - Auto-initializes git and reads author info from git config
- **CI/CD Workflows** - Generate GitHub Actions workflows for testing and releases
- **Unity Project Linking** - Auto-link packages to Unity projects with `--link`
- **Safety First** - Never auto-deletes existing directories

## Installation

### Prerequisites

- .NET SDK 9.0 or higher
- Linux, macOS, or Windows

### Quick Install

```bash
# Clone the repository
git clone https://github.com/IAFahim/PackageSmith.git
cd PackageSmith

# Run the installer
./install.sh        # Linux/macOS
.\install.ps1       # Windows
```

### Manual Install

```bash
# Build
dotnet build -c Release

# Run directly
dotnet run --project src/PackageSmith.App/PackageSmith.App.csproj
```

## Usage

### Interactive Mode

Run without arguments for the interactive menu:

```bash
pksmith
```

### Create a New Package

```bash
# From template
pksmith new com.mycompany.mypackage --template ecs -o ./Packages

# With auto-link to Unity project
pksmith new com.mycompany.mypackage --template ecs --link

# From scratch (basic structure)
pksmith new com.mycompany.mypackage
```

### List Templates

```bash
pksmith templates
```

### Generate CI Workflows

```bash
pksmith ci generate -o ./my-package
```

## Commands

| Command | Description |
|---------|-------------|
| `new [name]` | Create a new Unity package |
| `templates` | List available templates |
| `settings` | Configure global settings |
| `ci generate` | Generate CI/CD workflows |

### Options

| Option | Description |
|--------|-------------|
| `-t, --template <name>` | Use a template |
| `-o, --output <path>` | Output directory |
| `-d, --display <name>` | Display name |
| `-l, --link` | Link to Unity project |

## Template System

### Harvest a Template

Convert any existing Unity package into a reusable template:

```bash
# Using the Console project
dotnet run --project src/PackageSmith.Console/PackageSmith.Console.csproj harvest \
  /path/to/source/package my-template-name
```

Templates are stored in:
- Linux: `~/.local/share/PackageSmith/Templates/`
- macOS: `~/Library/Application Support/PackageSmith/Templates/`
- Windows: `%LOCALAPPDATA%\PackageSmith\Templates/`

### Template Features

- **Tokenization** - `{{PACKAGE_NAME}}`, `{{ASM_NAME}}`, `{{ASM_SHORT_NAME}}` replaced during generation
- **versionDefines Injection** - Unity package references get mandatory version defines
- **Assembly Name Sanitization** - Hyphens and underscores removed for valid C# identifiers

## Example Workflow

```bash
# 1. Create from template with Unity linking
pksmith new com.example.ecs-systems --template ecs --link

# 2. The package is now:
#    - Created in ./com.example.ecs-systems/
#    - Linked to your Unity project's Packages/manifest.json
#    - Git initialized
#    - Ready to use in Unity

# 3. Generate CI workflows for the package
cd com.example.ecs-systems
pksmith ci generate
```

## Package Structure

Generated packages include:

```
com.example.package/
├── package.json          # Unity package manifest
├── README.md             # Package documentation
├── CHANGELOG.md          # Version history
├── LICENSE.md            # License file
├── .gitignore            # Git ignore rules
├── .github/
│   └── workflows/
│       ├── test.yml      # Unity test workflow
│       └── release.yml   # GitHub release workflow
├── Runtime/              # Runtime assembly
│   └── Example.asmdef
├── Editor/               # Editor-only assembly
│   └── Example.Editor.asmdef
└── Samples~/             # Sample scenes and demos
```

## versionDefines

PackageSmith automatically adds `versionDefines` to `.asmdef` files for Unity packages:

| Package | Expression | Define |
|---------|------------|--------|
| `com.unity.input.system` | `1.7.0` | `UNITY_INPUT_SYSTEM_1_7_OR_NEWER` |
| `com.unity.render-pipelines.high-definition` | `7.1.0` | `HDRP_7_1_0_OR_NEWER` |
| `com.unity.render-pipelines.universal` | `14.0.0` | `URP_14_0_OR_NEWER` |
| `com.unity.burst` | `1.8.0` | `UNITY_BURST_EXISTS` |
| `com.unity.physics` | `1.0.0` | `UNITY_PHYSICS_MODULE_1_0_OR_NEWER` |
| And more... | | |

## Architecture

PackageSmith follows **Data-Oriented Design (DOD)** with a 4-layer architecture:

1. **Layer A: Pure Data** - Memory layout structs in `PackageSmith.Data`
2. **Layer B: Core Logic** - Stateless calculations in `PackageSmith.Core/Logic`
3. **Layer C: Extensions** - State validation and flow via extension methods
4. **Layer D: Bridges** - Unity integration via explicit interface implementation

```
src/
├── PackageSmith.Data/     # Layer A: State structs
├── PackageSmith.Core/     # Layers B & C: Logic & Extensions
└── PackageSmith.App/      # Layer D: CLI & Bridges
```

## Development

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run --project src/PackageSmith.App/PackageSmith.App.csproj
```

### Test

```bash
# Run CLI help
dotnet run --project src/PackageSmith.App/PackageSmith.App.csproj -- --help

# List templates
dotnet run --project src/PackageSmith.App/PackageSmith.App.csproj -- templates
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes following the DOD architecture
4. Submit a pull request

## License

MIT License - see LICENSE file for details

## Author

Created by IAFahim

## Acknowledgments

- Built with [Spectre.Console](https://spectreconsole.net/) for beautiful CLI output
- Inspired by the need for professional Unity package tooling
- Follows Data-Oriented Design principles for performance and clarity
