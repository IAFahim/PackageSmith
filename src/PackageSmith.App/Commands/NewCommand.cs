using System;
using System.IO;
using Spectre.Console;
using Spectre.Console.Cli;
using PackageSmith.App.Bridges;
using PackageSmith.Data.State;
using PackageSmith.Data.Types;
using PackageSmith.Core.Logic;

namespace PackageSmith.App.Commands;

public sealed class NewCommand : Command<NewCommand.Settings>
{
	public sealed class Settings : CommandSettings
	{
		[CommandArgument(0, "[name]")]
		public string? PackageName { get; init; }

		[CommandOption("-d|--display")]
		public string? DisplayName { get; init; }

		[CommandOption("-o|--output")]
		public string? OutputPath { get; init; }

		[CommandOption("-t|--template")]
		public string? TemplateName { get; init; }

		[CommandOption("-l|--link")]
		public bool LinkToUnity { get; init; }
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		if (!string.IsNullOrEmpty(settings.TemplateName)) // If template is specified, use template generator
		{
			return CreateFromTemplate(settings);
		}

		return CreateStandard(settings); // Otherwise use standard package creation
	}

	private int CreateFromTemplate(Settings settings)
	{
		AnsiConsole.MarkupLine("[dim]Creating package from template...[/]");

		var packageName = settings.PackageName ?? "com.company.newpackage";
		var templateName = settings.TemplateName!;
		var outputPath = settings.OutputPath ?? ".";

		var templatesDir = GetAppDataPath(); // Find template in AppData
		var templatePath = Path.Combine(templatesDir, "PackageSmith", "Templates", templateName);

		if (!Directory.Exists(templatePath))
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] Template '[cyan]{templateName}[/]' not found.");
			AnsiConsole.MarkupLine($"[dim]Expected path:[/] {templatePath}");
			AnsiConsole.MarkupLine("\n[dim]Available templates:[/]");
			ListTemplates();
			return 1;
		}

		var fullOutputPath = Path.Combine(outputPath, packageName); // Generate package from template

		if (!TemplateGeneratorLogic.TryGenerateFromTemplate(templatePath, fullOutputPath, packageName, out var fileCount))
		{
			AnsiConsole.MarkupLine("[red]Error:[/] Failed to generate from template");
			return 1;
		}

		AnsiConsole.MarkupLine($"[green]Success:[/] Created {fileCount} files from '[cyan]{templateName}[/]'");
		AnsiConsole.MarkupLine($"[dim]Location:[/] {fullOutputPath}");

		GitLogic.TryInitGit(fullOutputPath, out var gitSuccess); // Initialize git if available
		if (gitSuccess) AnsiConsole.MarkupLine("[dim]Git initialized[/]");

		if (settings.LinkToUnity) // Link to Unity project if requested
		{
			if (UnityLinkLogic.TryFindUnityProject(fullOutputPath, out var unityPath))
			{
				if (UnityLinkLogic.TryLinkToUnityProject(unityPath, fullOutputPath, packageName))
				{
					AnsiConsole.MarkupLine($"[green]Linked:[/] Added to Unity project at {unityPath}");
				}
				else
				{
					AnsiConsole.MarkupLine("[yellow]Warning:[/] Failed to link to Unity project");
				}
			}
			else
			{
				AnsiConsole.MarkupLine("[yellow]Warning:[/] No Unity project found in parent directories");
			}
		}

		return 0;
	}

	private int CreateStandard(Settings settings)
	{
		AnsiConsole.MarkupLine("[dim]Creating new Unity package...[/]");

		var bridge = new PackageBridge();

		var package = new PackageState
		{
			PackageName = settings.PackageName ?? "com.company.newpackage",
			DisplayName = settings.DisplayName ?? "New Package",
			Description = "A new Unity package",
			OutputPath = settings.OutputPath ?? ".",
			CompanyName = "YourCompany",
			UnityVersion = "2022.3",
			SelectedModules = PackageModuleType.Runtime | PackageModuleType.Editor,
			EcsPreset = new EcsPresetState { EnableEntities = false },
			SubAssemblies = SubAssemblyType.None,
			EnableSubAssemblies = false,
			DependencyCount = 0,
			SelectedTemplate = TemplateType.None
		};

		if (!bridge.TryCreate(in package))
		{
			AnsiConsole.MarkupLine("[red]Error:[/] Failed to create package");
			return 1;
		}

		PackageLogic.CombinePath(package.OutputPath, package.PackageName, out var fullPath);
		GitLogic.TryInitGit(fullPath, out var gitSuccess);

		AnsiConsole.MarkupLine($"[green]Success:[/] Package created");
		if (gitSuccess) AnsiConsole.MarkupLine("[dim]Git initialized[/]");

		if (settings.LinkToUnity) // Link to Unity project if requested
		{
			if (UnityLinkLogic.TryFindUnityProject(fullPath, out var unityPath))
			{
				if (UnityLinkLogic.TryLinkToUnityProject(unityPath, fullPath, package.PackageName))
				{
					AnsiConsole.MarkupLine($"[green]Linked:[/] Added to Unity project at {unityPath}");
				}
				else
				{
					AnsiConsole.MarkupLine("[yellow]Warning:[/] Failed to link to Unity project");
				}
			}
			else
			{
				AnsiConsole.MarkupLine("[yellow]Warning:[/] No Unity project found in parent directories");
			}
		}

		return 0;
	}

	private void ListTemplates()
	{
		var templatesDir = GetAppDataPath();
		var templatesPath = Path.Combine(templatesDir, "PackageSmith", "Templates");

		if (!Directory.Exists(templatesPath))
		{
			AnsiConsole.MarkupLine("  [dim]No templates found[/]");
			return;
		}

		foreach (var t in Directory.GetDirectories(templatesPath))
		{
			var name = Path.GetFileName(t);
			AnsiConsole.MarkupLine($"  • [cyan]{name}[/]");
		}
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
