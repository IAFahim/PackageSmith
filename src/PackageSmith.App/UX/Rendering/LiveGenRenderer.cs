using Spectre.Console;
using PackageSmith.Data.State;

namespace PackageSmith.App.UX.Rendering;

public static class LiveGenRenderer
{
	public static void RenderLive(in PackageLayoutState layout, string outputPath, string packageName)
	{
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[steelblue]INFO:[/] Creating package structure...");
		AnsiConsole.WriteLine();

		var packagePath = System.IO.Path.Combine(outputPath, packageName);

		var root = new Tree($"[steelblue]{System.IO.Path.GetFileName(packagePath)}/[/]");
		root.AddNode($"[grey]Generated {layout.DirectoryCount} directories[/]");
		root.AddNode($"[grey]Generated {layout.FileCount} files[/]");

		AnsiConsole.Write(root);
		AnsiConsole.MarkupLine($"\n[green]SUCCESS:[/] Package created at {packagePath}");
		AnsiConsole.WriteLine();
	}
}
