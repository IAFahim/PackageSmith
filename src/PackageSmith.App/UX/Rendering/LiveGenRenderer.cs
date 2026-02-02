using System.IO;
using PackageSmith.Data.State;
using Spectre.Console;

namespace PackageSmith.App.UX.Rendering;

public static class LiveGenRenderer
{
    public static void RenderLive(in PackageLayoutState layout, string outputPath, string packageName)
    {
        var packagePath = Path.Combine(outputPath, packageName);

        var root = new Tree($"[bold white]{Path.GetFileName(packagePath)}/[/]");
        root.AddNode($"[dim]{layout.DirectoryCount} dirs, {layout.FileCount} files[/]");

        AnsiConsole.Write(root);
        AnsiConsole.MarkupLine($"\n[green]Created[/] {packagePath}\n");
    }
}