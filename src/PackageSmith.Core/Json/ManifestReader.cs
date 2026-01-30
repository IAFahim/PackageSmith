using System.Text.Json;
using PackageSmith.Core.Models;

namespace PackageSmith.Core.Json;

public static class ManifestReader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true
    };

    public static bool TryReadManifest(string manifestPath, out UnityManifest manifest)
    {
        manifest = default;

        if (string.IsNullOrEmpty(manifestPath)) return false;
        if (!File.Exists(manifestPath)) return false;

        try
        {
            var json = File.ReadAllText(manifestPath);
            UnityManifest? parsed = JsonSerializer.Deserialize<UnityManifest>(json, Options);

            if (parsed == null) return false;

            manifest = parsed.Value;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryWriteManifest(string manifestPath, ref UnityManifest manifest)
    {
        if (string.IsNullOrEmpty(manifestPath)) return false;
        if (!File.Exists(manifestPath)) return false;

        try
        {
            var json = JsonSerializer.Serialize(manifest, Options);
            File.WriteAllText(manifestPath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryAddDependency(ref UnityManifest manifest, string packageName, string version)
    {
        if (manifest.Dependencies == null)
        {
            manifest.Dependencies = new Dictionary<string, string>();
        }

        if (manifest.Dependencies.ContainsKey(packageName))
        {
            manifest.Dependencies[packageName] = version;
        }
        else
        {
            manifest.Dependencies.Add(packageName, version);
        }

        return true;
    }

    public static bool TryRemoveDependency(ref UnityManifest manifest, string packageName)
    {
        if (manifest.Dependencies == null) return false;

        return manifest.Dependencies.Remove(packageName);
    }
}
