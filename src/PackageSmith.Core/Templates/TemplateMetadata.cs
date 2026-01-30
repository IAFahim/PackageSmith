using System.Text.Json.Serialization;

namespace PackageSmith.Core.Templates;

public class TemplateMetadata
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("author")]
    public string Author { get; set; } = "PackageSmith";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("variables")]
    public Dictionary<string, TemplateVariable> Variables { get; set; } = new();

    [JsonPropertyName("modules")]
    public List<string> Modules { get; set; } = new();

    [JsonPropertyName("dependencies")]
    public TemplateDependencies Dependencies { get; set; } = new();

    [JsonPropertyName("assemblyDependencies")]
    public Dictionary<string, List<string>> AssemblyDependencies { get; set; } = new();

    [JsonPropertyName("internalsVisibleTo")]
    public Dictionary<string, List<string>> InternalsVisibleTo { get; set; } = new();

    [JsonPropertyName("builtIn")]
    public bool BuiltIn { get; set; } = false;
}

public class TemplateVariable
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("required")]
    public bool Required { get; set; } = false;

    [JsonPropertyName("default")]
    public object? Default { get; set; }

    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }

    [JsonPropertyName("options")]
    public List<string>? Options { get; set; }
}

public class TemplateDependencies
{
    [JsonPropertyName("unity")]
    public string Unity { get; set; } = "2022.3";

    [JsonPropertyName("packages")]
    public List<string> Packages { get; set; } = new();
}
