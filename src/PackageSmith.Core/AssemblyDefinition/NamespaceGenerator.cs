namespace PackageSmith.Core.AssemblyDefinition;

public static class NamespaceGenerator
{
    public static string FromPackageName(string packageName, string? subFolder = null)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return "UnknownNamespace";

        var parts = packageName.Split('.');
        var ns = string.Join(".", parts.Select(p => ToPascalCase(p)));

        if (!string.IsNullOrWhiteSpace(subFolder))
        {
            ns = $"{ns}.{ToPascalCase(subFolder)}";
        }

        return ns;
    }

    public static string FromPackageName(string packageName, SubAssemblyType subAssembly)
    {
        return FromPackageName(packageName, subAssembly.ToString());
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return char.ToUpper(input[0]) + (input.Length > 1 ? input.Substring(1) : string.Empty);
    }

    public static string GenerateScript(string packageName, string subFolder, string className, string? content = null)
    {
        var ns = FromPackageName(packageName, subFolder);
        var body = content ?? "// Your code here";

        return $$"""
        namespace {{ns}};

        public class {{className}}
        {
            {{body}}
        }
        """;
    }

    public static string[] GetCommonNamespaces(string packageName)
    {
        var baseNs = FromPackageName(packageName);
        return new[]
        {
            baseNs,
            $"{baseNs}.Core",
            $"{baseNs}.Data",
            $"{baseNs}.Authoring",
            $"{baseNs}.Runtime",
            $"{baseNs}.Systems",
            $"{baseNs}.Debug"
        };
    }
}
