namespace PackageSmith.Core.Services;

public interface IUnityProjectFinder
{
    bool TryFindUnityProject(string startPath, out string projectPath);
    bool TryGetPackagesPath(string projectPath, out string packagesPath);
    bool TryGetManifestPath(string projectPath, out string manifestPath);
}
