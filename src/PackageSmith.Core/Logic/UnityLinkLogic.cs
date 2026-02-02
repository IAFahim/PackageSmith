using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace PackageSmith.Core.Logic;

public static class UnityLinkLogic
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryFindUnityProject(string startPath, out string projectPath)
    {
        projectPath = string.Empty;
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current != null)
        {
            var assets = Path.Combine(current.FullName, "Assets");
            var packages = Path.Combine(current.FullName, "Packages");
            var projectSettings = Path.Combine(current.FullName, "ProjectSettings");

            if (Directory.Exists(assets) && Directory.Exists(packages) && Directory.Exists(projectSettings))
            {
                projectPath = current.FullName;
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryLinkToUnityProject(string unityProjectPath, string packagePath, string packageName)
    {
        var manifestPath = Path.Combine(unityProjectPath, "Packages", "manifest.json");

        if (!File.Exists(manifestPath)) return false;

        try
        {
            var json = File.ReadAllText(manifestPath);
            var options = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip };
            var doc = JsonDocument.Parse(json, options);
            var root = doc.RootElement;

            var dependencies = new Dictionary<string, string>();

            if (root.TryGetProperty("dependencies", out var depsProp))
                foreach (var dep in depsProp.EnumerateObject())
                    dependencies[dep.Name] = dep.Value.GetString() ?? string.Empty;

            var packagesDir = Path.Combine(unityProjectPath, "Packages");
            var relativePath = Path.GetRelativePath(packagesDir, packagePath).Replace("\\", "/");

            dependencies[packageName] = $"file:{relativePath}";

            var newJson = "{\n  \"dependencies\": {";
            var first = true;
            foreach (var (name, version) in dependencies)
            {
                if (!first) newJson += ",";
                newJson += $"\n    \"{name}\": \"{version}\"";
                first = false;
            }

            newJson += "\n  }\n}\n";

            File.WriteAllText(manifestPath, newJson);
            return true;
        }
        catch
        {
            return false;
        }
    }
}