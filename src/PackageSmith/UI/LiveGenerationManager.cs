using Spectre.Console;
using PackageSmith.Core.Generation;

namespace PackageSmith.UI;

public static class LiveGenerationManager
{
    public static bool GenerateWithLivePreview(PackageLayout layout, string outputPath, string packageName)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[{StyleManager.InfoColor.ToMarkup()}]{StyleManager.IconInfo} Creating package structure...[/]");
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

        AnsiConsole.MarkupLine($"\n[{StyleManager.SuccessColor.ToMarkup()}]{StyleManager.IconSuccess} Package created successfully[/]");
        AnsiConsole.WriteLine();

        return true;
    }

    private static Tree CreateLiveTree(
        PackageLayout layout,
        string packagePath,
        HashSet<string> createdDirectories,
        HashSet<string> createdFiles)
    {
        var root = new Tree($"[{StyleManager.CommandColor.ToMarkup()}]{StyleManager.IconPackage} {Path.GetFileName(packagePath)}/[/]");

        foreach (var dir in layout.Directories.Skip(1))
        {
            var relativePath = GetRelativePath(dir.Path, packagePath);
            var isCreated = createdDirectories.Contains(dir.Path);
            var style = isCreated ? StyleManager.SuccessColor : StyleManager.MutedColor;
            var icon = isCreated ? StyleManager.IconSuccess : StyleManager.IconFolder;

            var node = root.AddNode($"[{style.ToMarkup()}]{icon} {relativePath}/[/]");

            var filesInDir = layout.Files.Where(f => Path.GetDirectoryName(f.Path) == dir.Path);
            foreach (var file in filesInDir)
            {
                var fileName = Path.GetFileName(file.Path);
                var fileCreated = createdFiles.Contains(file.Path);
                var fileStyle = fileCreated ? StyleManager.SuccessColor : StyleManager.MutedColor;
                var fileIcon = fileCreated ? StyleManager.IconSuccess : GetFileIcon(fileName);

                node.AddNode($"[{fileStyle.ToMarkup()}]{fileIcon} {fileName}[/]");
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
            var fileStyle = fileCreated ? StyleManager.SuccessColor : StyleManager.MutedColor;
            var fileIcon = fileCreated ? StyleManager.IconSuccess : GetFileIcon(fileName);

            root.AddNode($"[{fileStyle.ToMarkup()}]{fileIcon} {fileName}[/]");
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

    private static string GetFileIcon(string fileName)
    {
        return Path.GetExtension(fileName) switch
        {
            ".asmdef" => StyleManager.IconCode,
            ".cs" => StyleManager.IconCode,
            ".md" => StyleManager.IconInfo,
            ".json" => StyleManager.IconConfig,
            _ => StyleManager.IconDependency
        };
    }
}
