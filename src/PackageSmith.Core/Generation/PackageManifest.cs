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
        var keywordsArray = Keywords.Length > 0
            ? $"\n  \"keywords\": [{string.Join(", ", Keywords.Select(k => $"\"{k}\""))}],"
            : "";

        var deps = Dependencies.Length > 0
            ? GenerateDependenciesBlock()
            : "";

        var nugetDep = Dependencies.Any(d => d.Type.HasFlag(DependencyType.NuGet))
            ? $"\n  \"dependencies\": {{\n    \"com.unity.nuget.get\": \"3.1.0\"\n  }},"
            : "";

        return $$"""
        {
          "name": "{{Name}}",
          "version": "{{Version}}",
          "displayName": "{{DisplayName}}",
          "description": "{{Description}}",
          "unity": "{{Unity}}",
          "author": "{{Author}}",{{keywordsArray}}{{nugetDep}}
          "type": "tool"{{deps}}
        }
        """;
    }

    private readonly string GenerateDependenciesBlock()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(",\n  \"dependencies\": {");

        for (int i = 0; i < Dependencies.Length; i++)
        {
            var dep = Dependencies[i];
            sb.Append($"    {dep.ToPackageJsonDependency()}");

            if (i < Dependencies.Length - 1)
            {
                sb.AppendLine();
            }
        }

        sb.Append("\n  }");
        return sb.ToString();
    }
}
