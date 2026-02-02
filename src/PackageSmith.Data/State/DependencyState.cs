using System;
using System.Runtime.InteropServices;
using PackageSmith.Data.Types;

namespace PackageSmith.Data.State;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct DependencyState
{
    public string Name;
    public string Version;
    public DependencyType Type;
    public string Url;

    public readonly override string ToString()
    {
        return $"[Dep] {Name} {Version}";
    }
}