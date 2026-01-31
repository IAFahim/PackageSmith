using Spectre.Console;
using PackageSmith.Core.Generation;

namespace PackageSmith.UI;

public static class LiveGenerationManager
{
    public static bool GenerateWithLivePreview(PackageLayout layout, string outputPath, string packageName)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[{StyleManager.InfoColor.ToMarkup()}]{StyleManager.SymInfo} Creating package structure...[/]");
        AnsiConsole.WriteLine();

        var packagePath = Path.Combine(outputPath, packageName);

        var createdDirectories = new HashSet<string>();
        var createdFiles = new HashSet<string>();

        AnsiConsole.Live(CreateLiveTree(layout, packagePath, createdDirectories, createdFiles))
            .Start(ctx =>
            {
                ctx.Refresh();

                var totalItems = layout.Directories.Length + layout.Files.Length;

                for (int i = 0; i < totalItems; i++)
                {
                    if (i < layout.Directories.Length)
                    {
                        var dir = layout.Directories[i];
                        if (!Directory.Exists(dir.Path))
                        {
                            Directory.CreateDirectory(dir.Path);
                            createdDirectories.Add(dir.Path);
                        }
                    }
                    else
                    {
                        var fileIndex = i - layout.Directories.Length;
                        var file = layout.Files[fileIndex];
                        var directory = Path.GetDirectoryName(file.Path);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        File.WriteAllText(file.Path, file.Content);
                        createdFiles.Add(file.Path);
                    }

                    ctx.UpdateTarget(CreateLiveTree(layout, packagePath, createdDirectories, createdFiles));
                    ctx.Refresh();
                }
            });

        AnsiConsole.MarkupLine($"\n[{StyleManager.SuccessColor.ToMarkup()}]{StyleManager.SymTick} Package created successfully[/]");
        AnsiConsole.WriteLine();

        return true;
    }

    private static Tree CreateLiveTree(
        PackageLayout layout,
        string packagePath,
        HashSet<string> createdDirectories,
        HashSet<string> createdFiles)
    {
        var root = new Tree($"[{StyleManager.Primary.ToMarkup()}]{Path.GetFileName(packagePath)}/[/]");

        foreach (var dir in layout.Directories.Skip(1))
        {
            var relativePath = GetRelativePath(dir.Path, packagePath);
            var isCreated = createdDirectories.Contains(dir.Path);
            var color = isCreated ? StyleManager.SuccessColor : StyleManager.MutedColor;
            var prefix = isCreated ? StyleManager.SymTick : StyleManager.SymBullet;

            var node = root.AddNode($"[{color.ToMarkup()}]{prefix} {relativePath}/[/]");

            var filesInDir = layout.Files.Where(f => Path.GetDirectoryName(f.Path) == dir.Path);
            foreach (var file in filesInDir)
            {
                var fileName = Path.GetFileName(file.Path);
                var fileCreated = createdFiles.Contains(file.Path);
                var fileColor = fileCreated ? StyleManager.SuccessColor : StyleManager.MutedColor;
                var filePrefix = fileCreated ? StyleManager.SymTick : StyleManager.SymBullet;

                node.AddNode($"[{fileColor.ToMarkup()}]{filePrefix} {fileName}[/]");
            }
        }

        var rootFiles = layout.Files.Where(f =>
        {
            var dir = Path.GetDirectoryName(f.Path);
            return dir == packagePath;
        });

        foreach (var file in rootFiles)
        {
            var fileName = Path.GetFileName(file.Path);
            var fileCreated = createdFiles.Contains(file.Path);
            var fileColor = fileCreated ? StyleManager.SuccessColor : StyleManager.MutedColor;
            var filePrefix = fileCreated ? StyleManager.SymTick : StyleManager.SymBullet;

            root.AddNode($"[{fileColor.ToMarkup()}]{filePrefix} {fileName}[/]");
        }

        return root;
    }

    private static string GetRelativePath(string fullPath, string basePath)
    {
        if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        return fullPath;
    }
}
