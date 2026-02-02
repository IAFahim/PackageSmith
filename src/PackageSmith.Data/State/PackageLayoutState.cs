using System;
using System.Runtime.InteropServices;

namespace PackageSmith.Data.State;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct VirtualFileState
{
    public string Path;
    public string Content;
    public int ContentLength;

    public readonly override string ToString()
    {
        return $"[File] {Path}";
    }
}

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct VirtualDirectoryState
{
    public string Path;

    public readonly override string ToString()
    {
        return $"[Dir] {Path}";
    }
}

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct PackageLayoutState
{
    public int DirectoryCount;
    public int FileCount;

    public readonly override string ToString()
    {
        return $"[Layout] {DirectoryCount} dirs, {FileCount} files";
    }
}