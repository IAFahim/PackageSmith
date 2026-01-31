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

	public override readonly string ToString() => $"[File] {Path.ToString()}";
}

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct VirtualDirectoryState
{
	public string Path;

	public override readonly string ToString() => $"[Dir] {Path.ToString()}";
}

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct PackageLayoutState
{
	public int DirectoryCount;
	public int FileCount;

	public override readonly string ToString() => $"[Layout] {DirectoryCount} dirs, {FileCount} files";
}
