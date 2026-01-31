using System;
using System.Runtime.InteropServices;
using PackageSmith.Data.Types;

namespace PackageSmith.Data.State;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct DependencyState
{
	public FixedString64 Name;
	public FixedString64 Version;
	public DependencyType Type;
	public FixedString64 Url;

	public override readonly string ToString() => $"[Dep] {Name.ToString()} {Version.ToString()}";
}
