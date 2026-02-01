using System;
using System.IO;
using System.Linq;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PackageSmith.App.Commands;

public sealed class TemplatesCommand : Command<TemplatesCommand.Settings>
{
	public sealed class Settings : CommandSettings
	{
		[CommandOption("-v|--verbose")]
		public bool Verbose { get; init; }
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		var templatesDir = GetAppDataPath();
		var templatesPath = Path.Combine(templatesDir, "PackageSmith", "Templates");

		if (!Directory.Exists(templatesPath))
		{
			AnsiConsole.MarkupLine("[dim]No templates found.[/]");
			AnsiConsole.MarkupLine($"[dim]Templates directory:[/] {templatesPath}");
			return 0;
		}

		var templates = Directory.GetDirectories(templatesPath);

		if (templates.Length == 0)
		{
			AnsiConsole.MarkupLine("[dim]No templates found.[/]");
			return 0;
		}

		var table = new Table();
		table.Border(TableBorder.Minimal);
		table.AddColumn("[dim]Name[/]");
		table.AddColumn("[dim]Files[/]");
		table.AddColumn("[dim]Source[/]");

		foreach (var t in templates.OrderBy(x => x))
		{
			var name = Path.GetFileName(t);
			var manifestPath = Path.Combine(t, ".template.json");
			var fileCount = Directory.GetFiles(t, "*", SearchOption.AllDirectories).Where(f => !Path.GetFileName(f).StartsWith(".")).Count();
			var source = "Unknown";

			if (File.Exists(manifestPath))
			{
				try
				{
					var manifest = System.Text.Json.JsonDocument.Parse(File.ReadAllText(manifestPath));
					if (manifest.RootElement.TryGetProperty("sourcePackage", out var src))
						source = src.GetString() ?? "Unknown";
				}
				catch { /* Ignore */ }
			}

			table.AddRow($"[cyan]{name}[/]", $"[white]{fileCount}[/]", $"[dim]{source}[/]");
		}

		AnsiConsole.MarkupLine("[bold white]Available Templates[/]\n");
		AnsiConsole.Write(table);

		AnsiConsole.MarkupLine($"\n[dim]Usage: pksmith new <name> --template <template-name>[/]");

		return 0;
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
