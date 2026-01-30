namespace PackageSmith.Core.Dependencies;

[Serializable]
public readonly struct PackageDependency
{
    public readonly string Name;
    public readonly string? Version;
    public readonly DependencyType Type;
    public readonly string? Url;

    public PackageDependency(string name, string? version = null, DependencyType type = DependencyType.UnityPackage, string? url = null)
    {
        Name = name;
        Version = version;
        Type = type;
        Url = url;
    }

    public readonly string ToPackageJsonDependency()
    {
        if (Type.HasFlag(DependencyType.Git) && !string.IsNullOrWhiteSpace(Url))
        {
            return $"\"{Url}\",";
        }

        if (Type.HasFlag(DependencyType.NuGet))
        {
            return $"\"{Name}\": \"{{{Version ?? "latest"}}}\",";
        }

        return $"\"{Name}\": \"{Version ?? "1.0.0"}\",";
    }
}
