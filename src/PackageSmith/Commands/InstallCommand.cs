using Spectre.Console;
using Spectre.Console.Cli;
using PackageSmith.Core.Models;
using PackageSmith.Core.Services;
using PackageSmith.Core.Json;

namespace PackageSmith.Commands;

public class InstallCommand : Command<InstallCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[path]")]
        public string? Path { get; set; }

        [CommandOption("-p|--project <path>")]
        public string? ProjectPath { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var path = settings.Path;

        if (string.IsNullOrEmpty(path))
        {
            path = Directory.GetCurrentDirectory();
        }

        if (!Directory.Exists(path) && !File.Exists(path))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Path not found: {path}");
            return 1;
        }

        if (!PackageScanner.TryFindPackageJson(path, out var packageJsonPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] package.json not found in: {path}");
            return 1;
        }

        if (!PackageScanner.TryScanPackage(packageJsonPath, out var package))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Failed to parse package.json");
            return 1;
        }

        AnsiConsole.MarkupLine($"[cyan]Found:[/] {package}");

        var projectPath = settings.ProjectPath;

        if (string.IsNullOrEmpty(projectPath))
        {
            projectPath = Directory.GetCurrentDirectory();
        }

        if (!UnityProjectFinder.TryFindUnityProject(projectPath, out var unityProjectPath))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] Unity project not found in: {projectPath}");
            AnsiConsole.MarkupLine("[yellow]Hint:[/] Use -p to specify project path manually");
            return 1;
        }

        if (!UnityProjectFinder.TryGetPackagesPath(unityProjectPath, out var packagesPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Packages folder not found");
            return 1;
        }

        if (!UnityProjectFinder.TryGetManifestPath(unityProjectPath, out var manifestPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] manifest.json not found");
            return 1;
        }

        var targetDir = Path.GetDirectoryName(packageJsonPath) ?? path;
        var targetName = Path.GetFileName(targetDir);
        var destPath = Path.Combine(packagesPath, targetName);

        if (Directory.Exists(destPath))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] Package already exists at: {destPath}");
            if (!AnsiConsole.Confirm("Overwrite?"))
            {
                return 0;
            }
            Directory.Delete(destPath, recursive: true);
        }

        AnsiConsole.MarkupLine($"[green]Copying:[/] {targetDir} -> {destPath}");
        CopyDirectory(new DirectoryInfo(targetDir), new DirectoryInfo(destPath));

        if (!ManifestReader.TryReadManifest(manifestPath, out var manifest))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Failed to read manifest.json");
            return 1;
        }

        var fileVersion = $"file:{targetName}";
        ManifestReader.TryAddDependency(ref manifest, package.Name, fileVersion);

        if (!ManifestReader.TryWriteManifest(manifestPath, ref manifest))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Failed to write manifest.json");
            return 1;
        }

        AnsiConsole.MarkupLine($"[green]Success:[/] Added {package.Name} to manifest.json");
        AnsiConsole.MarkupLine($"[dim]Unity will detect the package on next restart[/dim]");

        return 0;
    }

    private static void CopyDirectory(DirectoryInfo source, DirectoryInfo target)
    {
        if (!target.Exists)
        {
            target.Create();
        }

        foreach (var file in source.GetFiles())
        {
            file.CopyTo(Path.Combine(target.FullName, file.Name), overwrite: true);
        }

        foreach (var dir in source.GetDirectories())
        {
            var nextTarget = target.CreateSubdirectory(dir.Name);
            CopyDirectory(dir, nextTarget);
        }
    }
}
