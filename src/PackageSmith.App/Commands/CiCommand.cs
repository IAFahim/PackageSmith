using System.IO;
using System.Linq;
using PackageSmith.Core.Extensions;
using PackageSmith.Core.Logic;
using PackageSmith.Data.State;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PackageSmith.App.Commands;

public sealed class CiCommand : Command<CiCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        var action = settings.Action ?? "generate";
        var outputPath = settings.OutputPath ?? ".";

        if (action.ToLowerInvariant() != "generate") return 1;

        AnsiConsole.MarkupLine("[dim]Analyzing package structure...[/]");

        if (!Directory.Exists(outputPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Path {outputPath} does not exist.");
            return 1;
        }

        var realFiles = Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories);
        var virtualFiles = realFiles.Select(f => new VirtualFileState
        {
            Path = Path.GetRelativePath(outputPath, f)
        }).ToArray();

        var layout = new PackageLayoutState { FileCount = virtualFiles.Length };
        AnalyzerLogic.AnalyzeLayout(in layout, virtualFiles, out var caps);

        AnsiConsole.MarkupLine($"[dim]Detected capabilities:[/] {caps}");

        if (caps.TryGenerateWorkflow(out var yaml))
        {
            var workflowsDir = Path.Combine(outputPath, ".github", "workflows");
            Directory.CreateDirectory(workflowsDir);

            var path = Path.Combine(workflowsDir, "test.yml");
            File.WriteAllText(path, yaml);

            AnsiConsole.MarkupLine($"[green]Success:[/] Generated smart workflow at {path}");
            if (caps.HasPlayModeTests) AnsiConsole.MarkupLine("  • [cyan]PlayMode[/] enabled");
            if (caps.HasEditModeTests) AnsiConsole.MarkupLine("  • [cyan]EditMode[/] enabled");
            if (caps.HasNativePlugins) AnsiConsole.MarkupLine("  • [yellow]Native Plugins[/] detected");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Warning:[/] Could not generate workflow.");
        }

        return 0;
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[action]")] public string? Action { get; init; }

        [CommandOption("-o|--output")] public string? OutputPath { get; init; }
    }
}