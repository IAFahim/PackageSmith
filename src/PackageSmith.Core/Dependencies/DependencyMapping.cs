namespace PackageSmith.Core.Dependencies;

[Serializable]
public readonly struct DependencyMapping
{
    public readonly string Namespace;
    public readonly string PackageName;
    public readonly string? GitUrl;
    public readonly DependencyType Type;

    public DependencyMapping(string ns, string packageName, string? gitUrl = null, DependencyType type = DependencyType.UnityPackage)
    {
        Namespace = ns;
        PackageName = packageName;
        GitUrl = gitUrl;
        Type = type;
    }
}

[Flags]
public enum DependencyType
{
    None = 0,
    UnityPackage = 1 << 0,
    NuGet = 1 << 1,
    Git = 1 << 2,
    Local = 1 << 3
}
