namespace PackageSmith.Core.Dependencies;

public sealed class DependencyResolver : IDependencyResolver
{
    private readonly DependencyRegistry _registry;
    private readonly List<PackageDependency> _resolvedDependencies = new();

    public DependencyResolver()
    {
        _registry = new DependencyRegistry();
    }

    public DependencyResolver(DependencyRegistry registry)
    {
        _registry = registry;
    }

    public bool TryResolveDependency(string ns, out PackageDependency dependency)
    {
        dependency = default;

        if (_registry.TryFindPackage(ns, out var mapping))
        {
            dependency = new PackageDependency(
                mapping.PackageName,
                null,
                mapping.Type,
                mapping.GitUrl
            );
            _resolvedDependencies.Add(dependency);
            return true;
        }

        return false;
    }

    public PackageDependency[] ResolveDependencies(string[] namespaces)
    {
        var results = new List<PackageDependency>();

        foreach (var ns in namespaces)
        {
            if (_registry.TryFindPackage(ns, out var mapping))
            {
                results.Add(new PackageDependency(
                    mapping.PackageName,
                    null,
                    mapping.Type,
                    mapping.GitUrl
                ));
            }
        }

        _resolvedDependencies.AddRange(results);
        return results.ToArray();
    }

    public PackageDependency[] GetAllResolved() => _resolvedDependencies.ToArray();
}
