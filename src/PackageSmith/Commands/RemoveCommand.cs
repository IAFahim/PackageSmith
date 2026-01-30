using Spectre.Console;
using Spectre.Console.Cli;
using PackageSmith.Core.Services;
using PackageSmith.Core.Json;

namespace PackageSmith.Commands;

public class RemoveCommand : Command<RemoveCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<name>")]
        public string? PackageName { get; set; }

        [CommandOption("-p|--project <path>")]
        public string? ProjectPath { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (string.IsNullOrEmpty(settings.PackageName))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Package name is required");
            return 1;
        }

        var projectPath = settings.ProjectPath ?? Directory.GetCurrentDirectory();

        if (!UnityProjectFinder.TryFindUnityProject(projectPath, out var unityProjectPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Unity project not found in: {projectPath}");
            return 1;
        }

        if (!UnityProjectFinder.TryGetManifestPath(unityProjectPath, out var manifestPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] manifest.json not found");
            return 1;
        }

        if (!ManifestReader.TryReadManifest(manifestPath, out var manifest))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Failed to read manifest.json");
            return 1;
        }

        if (manifest.Dependencies == null || !manifest.Dependencies.ContainsKey(settings.PackageName))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] Package '{settings.PackageName}' not found in manifest");
            return 1;
        }

        var version = manifest.Dependencies[settings.PackageName];

        if (version.StartsWith("file:"))
        {
            var localPath = version.Substring(5);
            var packagesPath = Path.Combine(unityProjectPath, "Packages");
            var fullPath = Path.Combine(packagesPath, localPath);

            if (Directory.Exists(fullPath))
            {
                AnsiConsole.MarkupLine($"[yellow]Deleting:[/] {fullPath}");
                Directory.Delete(fullPath, recursive: true);
            }
        }

        ManifestReader.TryRemoveDependency(ref manifest, settings.PackageName);

        if (!ManifestReader.TryWriteManifest(manifestPath, ref manifest))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Failed to write manifest.json");
            return 1;
        }

        AnsiConsole.MarkupLine($"[green]Success:[/] Removed {settings.PackageName} from manifest.json");
        AnsiConsole.MarkupLine($"[dim]Unity will detect the change on next restart[/dim]");

        return 0;
    }
}
