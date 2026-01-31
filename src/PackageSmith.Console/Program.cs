using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Spectre.Console;
using PackageSmith.Core.Logic;

namespace PackageSmith.Console;

public static class Program
{
	public static int Main(string[] args)
	{
		if (System.Console.IsOutputRedirected) return 0;

		AnsiConsole.MarkupLine("[bold cyan]◆[/] [bold white]PackageSmith Harvester[/]\n");

		// Quick test mode
		var sourcePath = "/mnt/5f79a6c2-0764-4cd7-88b4-12dbd1b39909/com.bovinelabs.bridge";
		var outputPath = "/tmp/PackageSmith_Harvested_Bridge";
		var packageName = "com.bovinelabs.bridge";

		AnsiConsole.MarkupLine($"[dim]Source:[/] {sourcePath}");
		AnsiConsole.MarkupLine($"[dim]Output:[/] {outputPath}\n");

		AnsiConsole.Status().Start("Harvesting...", ctx =>
		{
			ctx.Status("Scanning and tokenizing...");
			if (TemplateHarvesterLogic.TryHarvest(sourcePath, outputPath, packageName, out var count))
			{
				ctx.Status($"Done! Harvested {count} files");

				AnsiConsole.MarkupLine($"\n[green]Success![/] Harvested {count} files.");
				AnsiConsole.MarkupLine($"[dim]Template saved to:[/] [blue]{outputPath}[/]");

				// Show harvested files
				var files = Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories).OrderBy(x => x);
				AnsiConsole.MarkupLine($"\n[bold white]Harvested Files:[/]");

				var table = new Table();
				table.Border(TableBorder.Minimal);
				table.AddColumn(new TableColumn("[dim]Path[/]").Width(60));

				foreach (var f in files)
				{
					var rel = Path.GetRelativePath(outputPath, f);
					table.AddRow($"[white]{rel}[/]");
				}
				AnsiConsole.Write(table);
			}
			else
			{
				AnsiConsole.MarkupLine("\n[red]Harvest failed.[/]");
			}
		});

		return 0;
	}
}
