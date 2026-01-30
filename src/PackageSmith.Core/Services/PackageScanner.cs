using System.Text.Json;
using PackageSmith.Core.Models;

namespace PackageSmith.Core.Services;

public static class PackageScanner
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static bool TryFindPackageJson(string directory, out string packageJsonPath)
    {
        packageJsonPath = string.Empty;

        if (string.IsNullOrEmpty(directory)) return false;
        if (!Directory.Exists(directory)) return false;

        var path = directory.TrimEnd('/', '\\');

        while (true)
        {
            var testPath = Path.Combine(path, "package.json");
            if (File.Exists(testPath))
            {
                packageJsonPath = testPath;
                return true;
            }

            var parent = Directory.GetParent(path);
            if (parent == null) return false;

            path = parent.FullName;
        }
    }

    public static bool TryScanPackage(string path, out UnityPackage package)
    {
        package = default;

        if (string.IsNullOrEmpty(path)) return false;
        if (!File.Exists(path)) return false;

        try
        {
            var json = File.ReadAllText(path);
            UnityPackage? parsed = JsonSerializer.Deserialize<UnityPackage>(json, Options);

            if (parsed == null || string.IsNullOrEmpty(parsed.Value.Name)) return false;

            package = parsed.Value;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
