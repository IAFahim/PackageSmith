namespace PackageSmith.Core.Dependencies;

public interface IDependencyResolver
{
    bool TryResolveDependency(string ns, out PackageDependency dependency);
    PackageDependency[] ResolveDependencies(string[] namespaces);
}
