namespace PackageSmith.Core.AssemblyDefinition;

[Serializable]
public readonly struct SubAssemblyDefinition
{
    public readonly string Name;
    public readonly SubAssemblyType Type;
    public readonly string[] Dependencies;

    public SubAssemblyDefinition(string name, SubAssemblyType type, string[]? dependencies = null)
    {
        Name = name;
        Type = type;
        Dependencies = dependencies ?? Array.Empty<string>();
    }

    public static SubAssemblyDefinition[] GetStandardSubAssemblies(string packageName)
    {
        var baseName = packageName.Replace('.', '_');
        var prefix = $"{baseName}.";

        return new[]
        {
            new SubAssemblyDefinition($"{prefix}Core", SubAssemblyType.Core),
            new SubAssemblyDefinition($"{prefix}Data", SubAssemblyType.Data,
                new[] { $"{prefix}Core" }),
            new SubAssemblyDefinition($"{prefix}Authoring", SubAssemblyType.Authoring,
                new[] { $"{prefix}Core", $"{prefix}Data" }),
            new SubAssemblyDefinition($"{prefix}Runtime", SubAssemblyType.Runtime,
                new[] { $"{prefix}Core", $"{prefix}Data" }),
            new SubAssemblyDefinition($"{prefix}Systems", SubAssemblyType.Systems,
                new[] { $"{prefix}Core", $"{prefix}Data", $"{prefix}Runtime" }),
            new SubAssemblyDefinition($"{prefix}Debug", SubAssemblyType.Debug,
                new[] { $"{prefix}Core", $"{prefix}Data", $"{prefix}Runtime" })
        };
    }

    public readonly string GetFolderName() => Type.ToString();
}
