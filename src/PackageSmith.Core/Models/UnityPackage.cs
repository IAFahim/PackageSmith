using System.Text.Json.Serialization;

namespace PackageSmith.Core.Models;

public struct UnityPackage
{
    [JsonPropertyName("name")]
    public string Name;

    [JsonPropertyName("version")]
    public string Version;

    [JsonPropertyName("displayName")]
    public string DisplayName;

    [JsonPropertyName("description")]
    public string Description;

    [JsonPropertyName("unity")]
    public string Unity;

    [JsonPropertyName("dependencies")]
    public Dictionary<string, string> Dependencies;

    [JsonPropertyName("keywords")]
    public string[] Keywords;

    [JsonPropertyName("category")]
    public string Category;

    public readonly override string ToString() => $"[{Name}] {DisplayName} v{Version}";
}
