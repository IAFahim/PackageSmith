# iupk - Unity Package Installer

Install local Unity packages with a single command.

## Installation

### Windows (PowerShell)

```powershell
.\install.ps1
```

### Linux/Mac (Bash/Zsh)

```bash
./install.sh
```

## Usage

### Install a Package

Install from current directory (must contain `package.json`):

```bash
iupk install
```

Install from specific path:

```bash
iupk install ../MyPackage
```

Install to specific Unity project:

```bash
iupk install ../MyPackage -p /path/to/unity/project
```

### List Installed Packages

```bash
iupk list
```

### Remove a Package

```bash
iupk remove com.example.mypackage
```

### Update iupk

```bash
iupk update
```

## Requirements

- .NET SDK 8.0 or higher
- Unity project with `Packages/manifest.json`

## Development

Build the solution:

```bash
dotnet build PackageSmith.sln -c Release
```

Run directly:

```bash
dotnet run --project src/PackageSmith
```

## Uninstallation

### Windows

```powershell
.\uninstall.ps1
```

### Linux/Mac

```bash
./uninstall.sh
```

## License

MIT
