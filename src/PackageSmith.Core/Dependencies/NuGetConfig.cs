namespace PackageSmith.Core.Dependencies;

public static class NuGetConfig
{
    public const string NuGetForUnityUrl = "https://github.com/GlitchEnzo/NuGetForUnity.git";

    public static string GeneratePackagesConfig(PackageDependency[] dependencies)
    {
        var nugetDeps = dependencies.Where(d => d.Type.HasFlag(DependencyType.NuGet)).ToArray();

        if (nugetDeps.Length == 0)
        {
            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<packages>\n</packages>";
        }

        var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<packages>\n";
        foreach (var dep in nugetDeps)
        {
            xml += $"  <package id=\"{dep.Name}\" version=\"{dep.Version ?? "latest"}\" />\n";
        }
        xml += "</packages>";

        return xml;
    }

    public static bool RequiresNuGetForUnity(PackageDependency[] dependencies)
    {
        return dependencies.Any(d => d.Type.HasFlag(DependencyType.NuGet));
    }
}
