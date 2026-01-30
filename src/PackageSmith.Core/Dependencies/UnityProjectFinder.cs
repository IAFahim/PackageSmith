namespace PackageSmith.Core.Dependencies;

public sealed class UnityProjectFinder
{
    public static bool TryFindProjectRoot(string currentPath, out string projectRoot)
    {
        projectRoot = string.Empty;

        var dir = new DirectoryInfo(currentPath);
        while (dir.Parent is not null)
        {
            if (IsUnityProjectRoot(dir))
            {
                projectRoot = dir.FullName;
                return true;
            }

            dir = dir.Parent;
        }

        return false;
    }

    public static bool TryReadPackagesManifest(string projectRoot, out UnityPackagesManifest? manifest)
    {
        manifest = null;

        var manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");

        if (!File.Exists(manifestPath))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(manifestPath);

            // Simple check for dependencies (in production, use proper JSON parsing)
            var hasDependencies = json.Contains("\"dependencies\"");

            if (hasDependencies)
            {
                manifest = new UnityPackagesManifest(manifestPath, json);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsUnityProjectRoot(DirectoryInfo dir)
    {
        return dir.GetFiles("*.sln").Length > 0 ||
               dir.GetFiles("ProjectSettings").Length > 0 ||
               Directory.Exists(Path.Combine(dir.FullName, "Assets")) ||
               Directory.Exists(Path.Combine(dir.FullName, "Packages"));
    }
}

[Serializable]
public readonly struct UnityPackagesManifest
{
    public readonly string Path;
    public readonly string RawJson;

    public UnityPackagesManifest(string path, string rawJson)
    {
        Path = path;
        RawJson = rawJson;
    }

    public readonly bool HasDependency(string packageName)
    {
        return RawJson.Contains($"\"{packageName}\"");
    }
}
