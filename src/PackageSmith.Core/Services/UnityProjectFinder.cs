namespace PackageSmith.Core.Services;

public static class UnityProjectFinder
{
    private static readonly string[] UnityProjectMarkers = new[]
    {
        "ProjectSettings",
        "Assets",
        "Packages",
        "Library"
    };

    public static bool TryFindUnityProject(string startPath, out string projectPath)
    {
        projectPath = string.Empty;

        if (string.IsNullOrEmpty(startPath)) return false;

        var path = Path.GetFullPath(startPath);
        if (!Directory.Exists(path))
        {
            path = Directory.GetCurrentDirectory();
        }

        while (true)
        {
            if (IsUnityProject(path))
            {
                projectPath = path;
                return true;
            }

            var parent = Directory.GetParent(path);
            if (parent == null) return false;

            path = parent.FullName;
        }
    }

    public static bool TryGetPackagesPath(string projectPath, out string packagesPath)
    {
        packagesPath = string.Empty;

        if (string.IsNullOrEmpty(projectPath)) return false;
        if (!Directory.Exists(projectPath)) return false;

        packagesPath = Path.Combine(projectPath, "Packages");
        return Directory.Exists(packagesPath);
    }

    public static bool TryGetManifestPath(string projectPath, out string manifestPath)
    {
        manifestPath = string.Empty;

        if (!TryGetPackagesPath(projectPath, out var packagesPath)) return false;

        manifestPath = Path.Combine(packagesPath, "manifest.json");
        return File.Exists(manifestPath);
    }

    private static bool IsUnityProject(string path)
    {
        return UnityProjectMarkers.All(marker => Directory.Exists(Path.Combine(path, marker)));
    }
}
