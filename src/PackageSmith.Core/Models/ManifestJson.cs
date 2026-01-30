using System.Text.Json.Serialization;

namespace PackageSmith.Core.Models;

public struct UnityManifest
{
    [JsonPropertyName("dependencies")]
    public Dictionary<string, string> Dependencies;

    [JsonPropertyName("scopedRegistries")]
    public ScopedRegistry[]? ScopedRegistries;

    // Preserve additional fields to prevent data loss
    [JsonPropertyName("testables")]
    public string[]? Testables;

    [JsonPropertyName("registry")]
    public string? Registry;

    [JsonPropertyName("lock")]
    public Dictionary<string, object>? Lock;

    [JsonPropertyName("resolutionStrategy")]
    public string? ResolutionStrategy;

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData;

    public readonly override string ToString() => $"[Manifest] {Dependencies.Count:D} dependencies";
}

public struct ScopedRegistry
{
    [JsonPropertyName("name")]
    public string Name;

    [JsonPropertyName("url")]
    public string Url;

    [JsonPropertyName("scopes")]
    public string[] Scopes;
}
