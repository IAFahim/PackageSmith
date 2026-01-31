using System.Text.Json;
using System.Text.Json.Serialization;
using PackageSmith.Core.Dependencies;

namespace PackageSmith.Core.Generation;

[Serializable]
public readonly struct PackageManifest
{
    public readonly string Name;
    public readonly string Version;
    public readonly string DisplayName;
    public readonly string Description;
    public readonly string Unity;
    public readonly string Author;
    public readonly string[] Keywords;
    public readonly PackageDependency[] Dependencies;

    public PackageManifest(
        string name,
        string displayName,
        string description,
        string unity,
        string author,
        string[]? keywords = null,
        PackageDependency[]? dependencies = null)
    {
        Name = name;
        Version = "1.0.0";
        DisplayName = displayName;
        Description = description;
        Unity = unity;
        Author = author;
        Keywords = keywords ?? Array.Empty<string>();
        Dependencies = dependencies ?? Array.Empty<PackageDependency>();
    }

    public readonly string ToJson()
    {
        // Use JsonSerializer to ensure proper escaping of special characters
        // This prevents JSON injection when user input contains quotes or special chars
        var manifest = new PackageJsonDto
        {
            Name = Name,
            Version = Version,
            DisplayName = DisplayName,
            Description = Description,
            Unity = Unity,
            Author = Author,
            Keywords = Keywords.Length > 0 ? Keywords : null
        };

        // Add dependencies if any
        if (Dependencies.Length > 0)
        {
            manifest.Dependencies = new Dictionary<string, string>();
            foreach (var dep in Dependencies)
            {
                // Parse dependency format: "package.name": "version"
                var depJson = dep.ToPackageJsonDependency();
                var colonIndex = depJson.IndexOf(':');
                if (colonIndex > 0)
                {
                    var packageName = depJson.Substring(0, colonIndex).Trim().Trim('"');
                    var version = depJson.Substring(colonIndex + 1).Trim().Trim('"', ',', ' ');
                    manifest.Dependencies[packageName] = version;
                }
            }
        }

        // Add NuGet dependency if needed
        if (Dependencies.Any(d => d.Type.HasFlag(DependencyType.NuGet)))
        {
            manifest.Dependencies ??= new Dictionary<string, string>();
            manifest.Dependencies["com.unity.nuget.get"] = "3.1.0";
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        return JsonSerializer.Serialize(manifest, options);
    }

    // DTO for JSON serialization with proper escaping
    private class PackageJsonDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "";

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("unity")]
        public string Unity { get; set; } = "";

        [JsonPropertyName("author")]
        public string Author { get; set; } = "";

        [JsonPropertyName("keywords")]
        public string[]? Keywords { get; set; }

        [JsonPropertyName("dependencies")]
        public Dictionary<string, string>? Dependencies { get; set; }
    }
}
