using System;
using System.Runtime.InteropServices;
using PackageSmith.Data.Types;

namespace PackageSmith.Data.State;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct SubAssemblyState
{
	public FixedString64 Name;
	public SubAssemblyType Type;
	public int DependencyCount;

	public override readonly string ToString() => $"[SubAsm] {Name.ToString()} ({Type})";
}
