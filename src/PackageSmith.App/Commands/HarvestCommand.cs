using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using PackageSmith.Core.Logic;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PackageSmith.App.Commands;

public sealed class HarvestCommand : Command<HarvestCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        var sourcePath = Path.GetFullPath(settings.SourcePath);
        if (!Directory.Exists(sourcePath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Source directory not found: {sourcePath}");
            return 1;
        }

        var templateName = settings.TemplateName;
        string? packageName = null;

        if (string.IsNullOrEmpty(templateName))
        {
            var pkgJson = Path.Combine(sourcePath, "package.json");
            if (File.Exists(pkgJson))
                try
                {
                    var content = File.ReadAllText(pkgJson);
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("name", out var nameProp))
                    {
                        packageName = nameProp.GetString();
                        templateName = packageName;
                    }
                }
                catch
                {
                }

            if (string.IsNullOrEmpty(templateName)) templateName = new DirectoryInfo(sourcePath).Name;
        }

        AnsiConsole.MarkupLine($"[dim]Harvesting template from:[/] [cyan]{sourcePath}[/]");
        AnsiConsole.MarkupLine($"[dim]Target template name:[/] [cyan]{templateName}[/]");

        var templatesDir = GetAppDataPath();
        var outputDir = Path.Combine(templatesDir, "PackageSmith", "Templates", templateName!);

        if (Directory.Exists(outputDir))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] Template '[cyan]{templateName}[/]' already exists.");
            if (!AnsiConsole.Confirm("Overwrite?")) return 0;
        }

        var keepMeta = settings.KeepMeta ??
                       AnsiConsole.Confirm("Keep [cyan].meta[/] files? (Useful for full project harvesting)");

        if (TemplateHarvesterLogic.TryHarvest(sourcePath, outputDir, packageName ?? templateName!, out var count,
                keepMeta))
        {
            AnsiConsole.MarkupLine($"[green]Success:[/] Harvested {count} files to template '[cyan]{templateName}[/]'");

            GenerateManifest(outputDir, templateName!, packageName ?? templateName!, count);

            return 0;
        }

        AnsiConsole.MarkupLine("[red]Error:[/] Harvesting failed.");
        return 1;
    }

    private void GenerateManifest(string outputDir, string displayName, string sourcePackage, int fileCount)
    {
        var manifestPath = Path.Combine(outputDir, ".template.json");
        var json = $$"""
                     {
                       "id": "{{displayName}}",
                       "displayName": "{{displayName}}",
                       "sourcePackage": "{{sourcePackage}}",
                       "fileCount": {{fileCount}},
                       "harvestedAt": "{{DateTime.UtcNow:O}}"
                     }
                     """;
        File.WriteAllText(manifestPath, json);
    }

    private static string GetAppDataPath()
    {
        if (OperatingSystem.IsWindows())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        if (OperatingSystem.IsMacOS())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library",
                "Application Support");
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<source>")]
        [Description("Path to the package to harvest")]
        public string SourcePath { get; init; } = "";

        [CommandArgument(1, "[template-name]")]
        [Description("Name of the created template (defaults to package name)")]
        public string? TemplateName { get; init; }

        [CommandOption("--keep-meta")]
        [Description("Preserve .meta files in the template")]
        public bool? KeepMeta { get; init; }
    }
}