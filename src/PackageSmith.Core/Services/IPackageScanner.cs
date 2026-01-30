namespace PackageSmith.Core.Services;

public interface IPackageScanner
{
    bool TryScanPackage(string path, out Core.Models.UnityPackage package);
    bool TryFindPackageJson(string directory, out string packageJsonPath);
}
