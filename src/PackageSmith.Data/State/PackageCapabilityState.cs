using System;
using System.Runtime.InteropServices;

namespace PackageSmith.Data.State;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct PackageCapabilityState
{
	public bool HasPlayModeTests;
	public bool HasEditModeTests;
	public bool HasNativePlugins;
	public bool HasWebGL;

	public override readonly string ToString() => $"[Caps] Play:{HasPlayModeTests} Edit:{HasEditModeTests} Native:{HasNativePlugins}";
}