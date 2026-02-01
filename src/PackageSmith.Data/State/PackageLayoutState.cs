using System;
using System.Runtime.InteropServices;
using PackageSmith.Data.Types;

namespace PackageSmith.Data.State;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct VirtualFileState
{
	public string Path;
	public string Content;
	public int ContentLength;

	public readonly override string ToString() => $"[File] {Path}";
}

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct VirtualDirectoryState
{
	public string Path;

	public readonly override string ToString() => $"[Dir] {Path}";
}

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct PackageLayoutState
{
	public int DirectoryCount;
	public int FileCount;

	public readonly override string ToString() => $"[Layout] {DirectoryCount} dirs, {FileCount} files";
}
