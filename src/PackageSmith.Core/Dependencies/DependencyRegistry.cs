namespace PackageSmith.Core.Dependencies;

public sealed class DependencyRegistry
{
    private readonly List<DependencyMapping> _mappings = new();

    public DependencyRegistry()
    {
        // Add some common Unity packages
        AddMapping("Unity.Entities", "com.unity.entities", DependencyType.UnityPackage);
        AddMapping("Unity.Burst", "com.unity.burst", DependencyType.UnityPackage);
        AddMapping("Unity.Collections", "com.unity.collections", DependencyType.UnityPackage);
        AddMapping("Unity.Mathematics", "com.unity.mathematics", DependencyType.UnityPackage);
        AddMapping("Unity.Jobs", "com.unity.jobs", DependencyType.UnityPackage);
    }

    public void AddMapping(string ns, string packageName, DependencyType type = DependencyType.UnityPackage, string? gitUrl = null)
    {
        _mappings.Add(new DependencyMapping(ns, packageName, gitUrl, type));
    }

    public bool TryFindPackage(string ns, out DependencyMapping mapping)
    {
        // Exact match first
        foreach (var m in _mappings)
        {
            if (m.Namespace.Equals(ns, StringComparison.OrdinalIgnoreCase))
            {
                mapping = m;
                return true;
            }
        }

        // Prefix match (e.g., "GameVariable.Intent" matches "GameVariable")
        foreach (var m in _mappings)
        {
            if (ns.StartsWith(m.Namespace, StringComparison.OrdinalIgnoreCase))
            {
                mapping = m;
                return true;
            }
        }

        mapping = default;
        return false;
    }

    public DependencyMapping[] GetAllMappings() => _mappings.ToArray();
}
