using System;
using System.Runtime.InteropServices;
using PackageSmith.Data.Types;

namespace PackageSmith.Data.State;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct SubAssemblyState
{
	public string Name;
	public SubAssemblyType Type;
	public int DependencyCount;

	public readonly override string ToString() => $"[SubAsm] {Name} ({Type})";
}
