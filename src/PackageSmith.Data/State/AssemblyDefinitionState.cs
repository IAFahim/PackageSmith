using System;
using System.Runtime.InteropServices;
using PackageSmith.Data.Types;

namespace PackageSmith.Data.State;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct AssemblyDefinitionState
{
	public string Name;
	public bool AllowUnsafeCode;
	public bool OverrideReferences;
	public bool AutoReferenced;
	public bool NoEngineReferences;
	public int ReferenceCount;
	public int IncludePlatformCount;
	public int ExcludePlatformCount;

	public override readonly string ToString() => $"[AsmDef] {Name.ToString()}";
}
