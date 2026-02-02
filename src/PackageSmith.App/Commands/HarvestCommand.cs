using System;
using System.IO;
using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using PackageSmith.Core.Logic;

namespace PackageSmith.App.Commands;

public sealed class HarvestCommand : Command<HarvestCommand.Settings>
{
	public sealed class Settings : CommandSettings
	{
		[CommandArgument(0, "<source>")]
		[System.ComponentModel.Description("Path to the package to harvest")]
		public string SourcePath { get; init; } = "";

		[CommandArgument(1, "[template-name]")]
		[System.ComponentModel.Description("Name of the created template (defaults to package name)")]
		public string? TemplateName { get; init; }

		[CommandOption("--keep-meta")]
		[System.ComponentModel.Description("Preserve .meta files in the template")]
		public bool? KeepMeta { get; init; }
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		var sourcePath = Path.GetFullPath(settings.SourcePath);
		if (!Directory.Exists(sourcePath))
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] Source directory not found: {sourcePath}");
			return 1;
		}

		// Infer template name if not provided
		var templateName = settings.TemplateName;
		string? packageName = null;

		if (string.IsNullOrEmpty(templateName))
		{
			// Try to read package.json for the package name
			var pkgJson = Path.Combine(sourcePath, "package.json");
			if (File.Exists(pkgJson))
			{
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
				catch { /* Ignore */ }
			}

			// Fallback to directory name
			if (string.IsNullOrEmpty(templateName))
			{
				templateName = new DirectoryInfo(sourcePath).Name;
			}
		}

		AnsiConsole.MarkupLine($"[dim]Harvesting template from:[/] [cyan]{sourcePath}[/]");
		AnsiConsole.MarkupLine($"[dim]Target template name:[/] [cyan]{templateName}[/]");

		var templatesDir = GetAppDataPath();
		var outputDir = Path.Combine(templatesDir, "PackageSmith", "Templates", templateName!);

		// Check if template already exists
		if (Directory.Exists(outputDir))
		{
			AnsiConsole.MarkupLine($"[yellow]Warning:[/] Template '[cyan]{templateName}[/]' already exists.");
			if (!AnsiConsole.Confirm("Overwrite?"))
			{
				return 0;
			}
		}

		// Ask to keep meta files if not specified flag
		bool keepMeta = settings.KeepMeta ?? AnsiConsole.Confirm("Keep [cyan].meta[/] files? (Useful for full project harvesting)");

		// Execute Harvest Logic
		// Note: The third argument 'sourcePackageName' is currently unused by logic but required by signature.
		// We pass the inferred packageName or templateName.
		if (TemplateHarvesterLogic.TryHarvest(sourcePath, outputDir, packageName ?? templateName!, out var count, keepMeta))
		{
			AnsiConsole.MarkupLine($"[green]Success:[/] Harvested {count} files to template '[cyan]{templateName}[/]'");
			
			// Generate .template.json manifest for the UI
			GenerateManifest(outputDir, templateName!, packageName ?? templateName!, count);
			
			return 0;
		}
		else
		{
			AnsiConsole.MarkupLine("[red]Error:[/] Harvesting failed.");
			return 1;
		}
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
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support");
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
	}
}
