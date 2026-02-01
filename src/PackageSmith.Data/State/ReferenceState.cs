using System;
using System.Runtime.InteropServices;
using PackageSmith.Data.Types;

namespace PackageSmith.Data.State;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct ReferenceState
{
	public string Name;
	public bool IsUnityReference;

	public override readonly string ToString() => $"[Ref] {Name} {(IsUnityReference ? "[Unity]" : "[Custom]")}";
}
