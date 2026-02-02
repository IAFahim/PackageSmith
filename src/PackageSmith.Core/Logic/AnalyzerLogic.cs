using System.Runtime.CompilerServices;
using PackageSmith.Data.State;

namespace PackageSmith.Core.Logic;

public static class AnalyzerLogic
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AnalyzeLayout(in PackageLayoutState layout, VirtualFileState[] files,
        out PackageCapabilityState caps)
    {
        caps = new PackageCapabilityState();

        for (var i = 0; i < files.Length; i++)
        {
            var path = files[i].Path;
            if (path.Contains("Tests") && path.Contains("PlayMode")) caps.HasPlayModeTests = true;
            if (path.Contains("Tests") && path.Contains("EditMode")) caps.HasEditModeTests = true;
            if (path.EndsWith(".dll") || path.EndsWith(".so") || path.EndsWith(".dylib")) caps.HasNativePlugins = true;
        }
    }
}