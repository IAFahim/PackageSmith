namespace PackageSmith.Core.AssemblyDefinition;

[Serializable]
public readonly struct AsmDefReference
{
    public readonly string Name;
    public readonly bool IsUnityReference;

    public AsmDefReference(string name, bool isUnityReference = false)
    {
        Name = name;
        IsUnityReference = isUnityReference;
    }

    public static AsmDefReference Unity(string name) => new(name, true);
    public static AsmDefReference Custom(string name) => new(name, false);
}
