using System;
using System.Runtime.InteropServices;

namespace PackageSmith.Data.State;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct ReferenceState
{
    public string Name;
    public bool IsUnityReference;

    public readonly override string ToString()
    {
        return $"[Ref] {Name} {(IsUnityReference ? "[Unity]" : "[Custom]")}";
    }
}