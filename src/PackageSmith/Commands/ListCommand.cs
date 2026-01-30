using Spectre.Console.Cli;
using PackageSmith.Core.Services;
using PackageSmith.Core.Json;
using Spectre.Console;

namespace PackageSmith.Commands;

public class ListCommand : Command<ListCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-p|--project <path>")]
        public string? ProjectPath { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
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

        if (manifest.Dependencies == null || manifest.Dependencies.Count == 0)
        {
            AnsiConsole.MarkupLine($"\n[yellow]No packages found in:[/] {unityProjectPath}\n");
            return 0;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[yellow]Package[/]");
        table.AddColumn("[cyan]Version[/]");

        foreach (var dep in manifest.Dependencies.OrderBy(x => x.Key))
        {
            var isLocal = dep.Value.StartsWith("file:");
            var versionStyle = isLocal ? "[green]" : "[blue]";
            table.AddRow(dep.Key, $"{versionStyle}{dep.Value}[/]");
        }

        AnsiConsole.MarkupLine($"\n[bold]Packages in:[/] {unityProjectPath}\n");
        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Total: {manifest.Dependencies.Count:D} packages[/]\n");

        return 0;
    }
}
