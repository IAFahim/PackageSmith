namespace PackageSmith.Core.Generation;

[Serializable]
public readonly struct VirtualDirectory
{
    public readonly string Path;

    public VirtualDirectory(string path)
    {
        Path = path;
    }

    public readonly bool IsValid => !string.IsNullOrWhiteSpace(Path);
}
