using System.ComponentModel;
using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PackageSmith.Commands;

[Description("Transfer packages between Library/PackageCache and Packages folders")]
public class TransferCommand : Command<TransferCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<package>")]
        [Description("Package name (e.g., com.company.package)")]
        public required string PackageName { get; set; }

        [CommandOption("-p|--project <PATH>")]
        [Description("Unity project path (default: search from current directory)")]
        public string? ProjectPath { get; set; }

        [CommandOption("--to-packages")]
        [Description("Move from Library/PackageCache to Packages (for editing)")]
        public bool ToPackages { get; set; }

        [CommandOption("--to-library")]
        [Description("Move from Packages to Library/PackageCache (for testing)")]
        public bool ToLibrary { get; set; }

        [CommandOption("--no-backup")]
        [Description("Skip creating backup before transfer")]
        public bool NoBackup { get; set; }

        [CommandOption("-f|--force")]
        [Description("Force transfer without confirmation")]
        public bool Force { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        return ExecuteTransfer(settings);
    }

    public int ExecuteTransfer(Settings settings)
    {
        // Auto-detect direction if not specified
        if (!settings.ToPackages && !settings.ToLibrary)
        {
            var detected = DetectPackageLocation(settings.PackageName, settings.ProjectPath);
            if (detected == PackageLocation.Library)
            {
                settings.ToPackages = true;
                AnsiConsole.MarkupLine($"[dim]Auto-detected: Moving from Library to Packages (editable mode)[/]");
            }
            else if (detected == PackageLocation.Packages)
            {
                settings.ToLibrary = true;
                AnsiConsole.MarkupLine($"[dim]Auto-detected: Moving from Packages to Library (testing mode)[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Error: Package not found in Library or Packages folder[/]");
                return 1;
            }
        }

        // Validate conflicting flags
        if (settings.ToPackages && settings.ToLibrary)
        {
            AnsiConsole.MarkupLine("[red]Error: Cannot specify both --to-packages and --to-library[/]");
            return 1;
        }

        var projectPath = FindUnityProject(settings.ProjectPath);
        if (projectPath == null)
        {
            AnsiConsole.MarkupLine("[red]Error: Unity project not found[/]");
            AnsiConsole.MarkupLine("[dim]Use --project to specify project path[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[dim]Unity Project:[/] {projectPath}");

        if (settings.ToPackages)
        {
            return MoveToPackages(settings, projectPath);
        }
        else
        {
            return MoveToLibrary(settings, projectPath);
        }
    }

    private int MoveToPackages(Settings settings, string projectPath)
    {
        var packageName = settings.PackageName;
        var libraryPath = Path.Combine(projectPath, "Library", "PackageCache", packageName);
        var packagesPath = Path.Combine(projectPath, "Packages", packageName);

        // Find package in Library (may have version suffix)
        if (!Directory.Exists(libraryPath))
        {
            var packageCacheDir = Path.Combine(projectPath, "Library", "PackageCache");
            if (!Directory.Exists(packageCacheDir))
            {
                AnsiConsole.MarkupLine("[red]Error: Library/PackageCache folder not found[/]");
                return 1;
            }

            var matches = Directory.GetDirectories(packageCacheDir, $"{packageName}@*");
            if (matches.Length == 0)
            {
                AnsiConsole.MarkupLine($"[red]Error: Package '{packageName}' not found in Library/PackageCache[/]");
                return 1;
            }

            libraryPath = matches[0];
            AnsiConsole.MarkupLine($"[dim]Found:[/] {Path.GetFileName(libraryPath)}");
        }

        // Check if already exists in Packages
        if (Directory.Exists(packagesPath))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Package already exists in Packages folder[/]");
            if (!settings.Force && !AnsiConsole.Confirm("Overwrite?", false))
            {
                return 0;
            }
        }

        // Confirm
        var panel = new Panel(new Markup($@"[yellow]Transfer Package to Packages Folder[/]

[dim]From:[/] Library/PackageCache/{Path.GetFileName(libraryPath)}
[dim]To:[/] Packages/{packageName}

This will:
  • Copy package to Packages/ for [green]local editing[/]
  • Update manifest.json to use [green]file:[/] reference
  • {(settings.NoBackup ? "[dim]No backup[/]" : "Create backup in Library/PackageCache.backup/")}

[yellow]Purpose:[/] Make package [green]editable[/] for development"))
            .BorderColor(Color.Yellow)
            .Header("[yellow]Confirm Transfer[/]");

        AnsiConsole.Write(panel);

        if (!settings.Force && !AnsiConsole.Confirm("\nProceed?", true))
        {
            AnsiConsole.MarkupLine("[dim]Transfer cancelled[/]");
            return 0;
        }

        return AnsiConsole.Status()
            .Start("Transferring package...", ctx =>
            {
                // Backup
                if (!settings.NoBackup)
                {
                    ctx.Status("Creating backup...");
                    CreateBackup(libraryPath, projectPath);
                    AnsiConsole.MarkupLine("[green]✓[/] Backup created");
                }

                // Copy to Packages
                ctx.Status("Copying package...");
                CopyDirectory(libraryPath, packagesPath);
                AnsiConsole.MarkupLine($"[green]✓[/] Copied to Packages/{packageName}");

                // Update manifest
                ctx.Status("Updating manifest.json...");
                UpdateManifestToFileReference(projectPath, packageName);
                AnsiConsole.MarkupLine("[green]✓[/] Updated manifest.json");

                // Remove from Library (optional cleanup)
                ctx.Status("Cleaning up...");
                AnsiConsole.MarkupLine($"[dim]Library package remains at: {libraryPath}[/]");
                AnsiConsole.MarkupLine($"[dim]Unity will ignore it in favor of Packages/{packageName}[/]");

                return 0;
            });
    }

    private int MoveToLibrary(Settings settings, string projectPath)
    {
        var packageName = settings.PackageName;
        var packagesPath = Path.Combine(projectPath, "Packages", packageName);

        if (!Directory.Exists(packagesPath))
        {
            AnsiConsole.MarkupLine($"[red]Error: Package '{packageName}' not found in Packages folder[/]");
            return 1;
        }

        // Confirm
        var panel = new Panel(new Markup($@"[yellow]Transfer Package to Library (Testing Mode)[/]

[dim]From:[/] Packages/{packageName}
[dim]Action:[/] Remove from Packages/, reference from registry

This will:
  • Remove package from Packages/ folder
  • Update manifest.json to use [cyan]registry[/] reference
  • Package will come from Library/PackageCache (as if installed)
  • {(settings.NoBackup ? "[dim]No backup[/]" : "Create backup in Packages.backup/")}

[yellow]Purpose:[/] Test package as if [cyan]installed from registry[/]"))
            .BorderColor(Color.Yellow)
            .Header("[yellow]Confirm Transfer[/]");

        AnsiConsole.Write(panel);

        if (!settings.Force && !AnsiConsole.Confirm("\nProceed?", true))
        {
            AnsiConsole.MarkupLine("[dim]Transfer cancelled[/]");
            return 0;
        }

        return AnsiConsole.Status()
            .Start("Transferring package...", ctx =>
            {
                // Backup
                if (!settings.NoBackup)
                {
                    ctx.Status("Creating backup...");
                    var backupPath = Path.Combine(projectPath, "Packages.backup", $"{packageName}.{DateTime.Now:yyyyMMdd-HHmmss}");
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                    CopyDirectory(packagesPath, backupPath);
                    AnsiConsole.MarkupLine($"[green]✓[/] Backup created at Packages.backup/");
                }

                // Read package.json to get version
                var packageJsonPath = Path.Combine(packagesPath, "package.json");
                string? version = null;
                if (File.Exists(packageJsonPath))
                {
                    var json = JsonDocument.Parse(File.ReadAllText(packageJsonPath));
                    version = json.RootElement.GetProperty("version").GetString();
                }

                // Update manifest to use registry reference
                ctx.Status("Updating manifest.json...");
                UpdateManifestToRegistryReference(projectPath, packageName, version);
                AnsiConsole.MarkupLine("[green]✓[/] Updated manifest.json");

                // Remove from Packages
                ctx.Status("Removing from Packages folder...");
                Directory.Delete(packagesPath, true);
                AnsiConsole.MarkupLine($"[green]✓[/] Removed from Packages/");

                AnsiConsole.MarkupLine("\n[yellow]Note:[/] Package will be downloaded to Library/PackageCache on next Unity refresh");

                return 0;
            });
    }

    private PackageLocation DetectPackageLocation(string packageName, string? projectPath)
    {
        var project = FindUnityProject(projectPath);
        if (project == null) return PackageLocation.NotFound;

        var packagesPath = Path.Combine(project, "Packages", packageName);
        if (Directory.Exists(packagesPath))
        {
            return PackageLocation.Packages;
        }

        var libraryPath = Path.Combine(project, "Library", "PackageCache", packageName);
        if (Directory.Exists(libraryPath))
        {
            return PackageLocation.Library;
        }

        // Check for versioned packages in Library
        var packageCacheDir = Path.Combine(project, "Library", "PackageCache");
        if (Directory.Exists(packageCacheDir))
        {
            var matches = Directory.GetDirectories(packageCacheDir, $"{packageName}@*");
            if (matches.Length > 0)
            {
                return PackageLocation.Library;
            }
        }

        return PackageLocation.NotFound;
    }

    private string? FindUnityProject(string? startPath)
    {
        var current = startPath != null ? new DirectoryInfo(startPath) : new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current != null)
        {
            var manifestPath = Path.Combine(current.FullName, "Packages", "manifest.json");
            if (File.Exists(manifestPath))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        return null;
    }

    private void CreateBackup(string sourcePath, string projectPath)
    {
        var backupDir = Path.Combine(projectPath, "Library", "PackageCache.backup");
        Directory.CreateDirectory(backupDir);

        var backupPath = Path.Combine(backupDir, $"{Path.GetFileName(sourcePath)}.{DateTime.Now:yyyyMMdd-HHmmss}");
        CopyDirectory(sourcePath, backupPath);
    }

    private void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            if (fileName == ".DS_Store") continue;
            File.Copy(file, Path.Combine(targetDir, fileName), true);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(directory);
            // Preserve .git folder during transfers
            if (dirName == ".DS_Store") continue;
            CopyDirectory(directory, Path.Combine(targetDir, dirName));
        }
    }

    private void UpdateManifestToFileReference(string projectPath, string packageName)
    {
        var manifestPath = Path.Combine(projectPath, "Packages", "manifest.json");
        var json = File.ReadAllText(manifestPath);
        var doc = JsonDocument.Parse(json);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name == "dependencies")
                {
                    writer.WritePropertyName("dependencies");
                    writer.WriteStartObject();

                    foreach (var dep in prop.Value.EnumerateObject())
                    {
                        if (dep.Name == packageName)
                        {
                            writer.WriteString(packageName, $"file:{packageName}");
                        }
                        else
                        {
                            dep.WriteTo(writer);
                        }
                    }

                    writer.WriteEndObject();
                }
                else
                {
                    prop.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
        }

        File.WriteAllText(manifestPath, System.Text.Encoding.UTF8.GetString(stream.ToArray()));
    }

    private void UpdateManifestToRegistryReference(string projectPath, string packageName, string? version)
    {
        var manifestPath = Path.Combine(projectPath, "Packages", "manifest.json");
        var json = File.ReadAllText(manifestPath);
        var doc = JsonDocument.Parse(json);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name == "dependencies")
                {
                    writer.WritePropertyName("dependencies");
                    writer.WriteStartObject();

                    foreach (var dep in prop.Value.EnumerateObject())
                    {
                        if (dep.Name == packageName)
                        {
                            writer.WriteString(packageName, version ?? "1.0.0");
                        }
                        else
                        {
                            dep.WriteTo(writer);
                        }
                    }

                    writer.WriteEndObject();
                }
                else
                {
                    prop.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
        }

        File.WriteAllText(manifestPath, System.Text.Encoding.UTF8.GetString(stream.ToArray()));
    }

    private enum PackageLocation
    {
        NotFound,
        Packages,
        Library
    }
}
