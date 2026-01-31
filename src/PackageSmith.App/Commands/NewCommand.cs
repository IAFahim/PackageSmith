using System;
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
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		AnsiConsole.MarkupLine("[steelblue]Creating new Unity package...[/]");

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
			AnsiConsole.MarkupLine("[red]ERROR:[/] Failed to create package");
			return 1;
		}

		PackageLogic.CombinePath(package.OutputPath, package.PackageName, out var fullPath);
		GitLogic.TryInitGit(fullPath, out var gitSuccess);

		AnsiConsole.MarkupLine("[steelblue]SUCCESS:[/] Package created successfully");
		if (gitSuccess) AnsiConsole.MarkupLine("[grey]Git repository initialized[/]");

		return 0;
	}
}
