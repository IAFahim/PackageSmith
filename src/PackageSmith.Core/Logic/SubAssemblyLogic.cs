using System.Collections.Generic;
using System.Runtime.CompilerServices;
using PackageSmith.Data.Types;

namespace PackageSmith.Core.Logic;

public static class SubAssemblyLogic
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] GetRuntimeAssemblies(SubAssemblyType subAssemblies, string asmdefRoot)
    {
        var list = new List<string>();

        if (subAssemblies.HasFlag(SubAssemblyType.Core))
            list.Add($"{asmdefRoot}.Core");
        if (subAssemblies.HasFlag(SubAssemblyType.Data))
            list.Add($"{asmdefRoot}.Data");
        if (subAssemblies.HasFlag(SubAssemblyType.Runtime))
            list.Add($"{asmdefRoot}.Runtime");
        if (subAssemblies.HasFlag(SubAssemblyType.Authoring))
            list.Add($"{asmdefRoot}.Authoring");

        return list.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] GetEditorAssemblies(SubAssemblyType subAssemblies, string asmdefRoot)
    {
        var list = new List<string>();

        if (subAssemblies.HasFlag(SubAssemblyType.Core))
            list.Add($"{asmdefRoot}.Core");
        if (subAssemblies.HasFlag(SubAssemblyType.Data))
            list.Add($"{asmdefRoot}.Data");
        if (subAssemblies.HasFlag(SubAssemblyType.Runtime))
            list.Add($"{asmdefRoot}.Runtime");
        if (subAssemblies.HasFlag(SubAssemblyType.Authoring))
            list.Add($"{asmdefRoot}.Authoring");
        if (subAssemblies.HasFlag(SubAssemblyType.Editor))
            list.Add($"{asmdefRoot}.Editor");

        return list.ToArray();
    }
}