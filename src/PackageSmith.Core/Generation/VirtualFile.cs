namespace PackageSmith.Core.Generation;

[Serializable]
public readonly struct VirtualFile
{
    public readonly string Path;
    public readonly string Content;

    public VirtualFile(string path, string content)
    {
        Path = path;
        Content = content;
    }

    public readonly bool IsValid => !string.IsNullOrWhiteSpace(Path) && Content is not null;
}
