using System;
using System.IO;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PackageSmith.App.Commands;

public sealed class CiCommand : Command<CiCommand.Settings>
{
	public sealed class Settings : CommandSettings
	{
		[CommandArgument(0, "[action]")]
		public string? Action { get; init; }

		[CommandOption("-o|--output")]
		public string? OutputPath { get; init; }
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		var action = settings.Action ?? "generate";
		var outputPath = settings.OutputPath ?? ".";

		return action.ToLowerInvariant() switch
		{
			"generate" => GenerateWorkflows(outputPath),
			_ => 1
		};
	}

	private int GenerateWorkflows(string outputPath)
	{
		AnsiConsole.MarkupLine("[dim]Generating CI/CD workflows...[/]");

		var workflowsDir = Path.Combine(outputPath, ".github", "workflows");
		Directory.CreateDirectory(workflowsDir);

		// Generate test workflow
		var testWorkflow = @"name: Test

on:
  push:
    branches: [ master, main ]
  pull_request:
    branches: [ master, main ]

jobs:
  test:
    name: Test on Unity 2022.3
    runs-on: ubuntu-latest
    container:
      image: unityci/editor:ubuntu-2022.3-windows-mono-1
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Cache Library
        uses: actions/cache@v4
        with:
          path: Library
          key: Library-2022.3-{{hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**')}}
          restore-keys: |
            Library-2022.3-
            Library-

      - name: Run Tests
        run: |
          unity-editor \
            -runTests \
            -testPlatformEditMode \
            -testResultsResults/EditModeResults.xml \
            -batchmode \
            -projectPath .

      - name: Upload Test Results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: Test Results
          path: Results/EditModeResults.xml
";

		var testPath = Path.Combine(workflowsDir, "test.yml");
		File.WriteAllText(testPath, testWorkflow);

		// Generate release workflow
		var releaseWorkflow = @"name: Release

on:
  push:
    tags:
      - 'v*'

jobs:
  release:
    name: Create Release
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Generate Release Notes
        id: release
        uses: actions/create-release@v1
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        with:
          tag_name: ${{ github.ref }}
          release_name: Release ${{ github.ref }}
          draft: false
          prerelease: false
";

		var releasePath = Path.Combine(workflowsDir, "release.yml");
		File.WriteAllText(releasePath, releaseWorkflow);

		AnsiConsole.MarkupLine($"[green]Success:[/] Generated 2 workflows in {workflowsDir}");
		AnsiConsole.MarkupLine("  • [cyan]test.yml[/] - Run Unity tests on push/PR");
		AnsiConsole.MarkupLine("  • [cyan]release.yml[/] - Create GitHub release on tag");

		return 0;
	}
}
