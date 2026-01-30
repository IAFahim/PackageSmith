using System.Text.Json.Serialization;

namespace PackageSmith.Core.Models;

public struct UnityManifest
{
    [JsonPropertyName("dependencies")]
    public Dictionary<string, string>? Dependencies { get; set; }

    [JsonPropertyName("scopedRegistries")]
    public ScopedRegistry[]? ScopedRegistries { get; set; }

    // Preserve additional fields to prevent data loss
    [JsonPropertyName("testables")]
    public string[]? Testables { get; set; }

    [JsonPropertyName("registry")]
    public string? Registry { get; set; }

    [JsonPropertyName("lock")]
    public Dictionary<string, object>? Lock { get; set; }

    [JsonPropertyName("resolutionStrategy")]
    public string? ResolutionStrategy { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    public readonly override string ToString() => $"[Manifest] {Dependencies?.Count ?? 0:D} dependencies";
}

public struct ScopedRegistry
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("scopes")]
    public string[] Scopes { get; set; }
}
