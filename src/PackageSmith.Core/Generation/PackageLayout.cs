namespace PackageSmith.Core.Generation;

[Serializable]
public readonly struct PackageLayout
{
    public readonly VirtualDirectory[] Directories;
    public readonly VirtualFile[] Files;

    public PackageLayout(VirtualDirectory[] directories, VirtualFile[] files)
    {
        Directories = directories;
        Files = files;
    }

    public readonly bool IsValid => Directories.Length > 0 && Files.Length > 0;
}
