using System;
using System.Runtime.InteropServices;

namespace PackageSmith.Data.Templates;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct TemplateManifest
{
	public string Id;
	public string DisplayName;
	public string Description;
	public string SourcePackageName;
	public int FileCount;
	public long TotalSize;
	public bool PreserveAsmRefs;
	public bool StripComments;

	public override readonly string ToString() => $"[Template] {DisplayName} ({FileCount} files)";
}
