namespace PackageSmith.Core.AssemblyDefinition;

[Serializable]
public readonly struct EcsPreset
{
    public readonly bool EnableEntities;
    public readonly bool EnableBurst;
    public readonly bool EnableCollections;
    public readonly bool EnableMathematics;
    public readonly bool EnableJobs;
    public readonly bool EnablePhysics;

    public static EcsPreset Full => new(true, true, true, true, true, false);
    public static EcsPreset None => new(false, false, false, false, false, false);

    public EcsPreset(
        bool enableEntities = false,
        bool enableBurst = false,
        bool enableCollections = false,
        bool enableMathematics = false,
        bool enableJobs = false,
        bool enablePhysics = false)
    {
        EnableEntities = enableEntities;
        EnableBurst = enableBurst;
        EnableCollections = enableCollections;
        EnableMathematics = enableMathematics;
        EnableJobs = enableJobs;
        EnablePhysics = enablePhysics;
    }

    public readonly AsmDefReference[] GetRuntimeReferences()
    {
        var refs = new List<AsmDefReference>();

        if (EnableEntities) refs.Add(AsmDefReference.Unity("Unity.Entities"));
        if (EnableBurst) refs.Add(AsmDefReference.Unity("Unity.Burst"));
        if (EnableCollections) refs.Add(AsmDefReference.Unity("Unity.Collections"));
        if (EnableMathematics) refs.Add(AsmDefReference.Unity("Unity.Mathematics"));
        if (EnableJobs) refs.Add(AsmDefReference.Unity("Unity.Jobs"));
        if (EnablePhysics) refs.Add(AsmDefReference.Unity("Unity.Physics"));
        if (EnableBurst) refs.Add(AsmDefReference.Unity("Unity.Burst.UniTask"));

        return refs.ToArray();
    }

    public readonly string[] GetDefineConstraints()
    {
        if (!EnableBurst && !EnableEntities) return Array.Empty<string>();

        var defines = new List<string>();
        if (EnableBurst) defines.Add("UNITY_BURST_AOT");
        return defines.ToArray();
    }

    public readonly bool IsEnabled => EnableEntities || EnableBurst || EnableCollections || EnableMathematics || EnableJobs || EnablePhysics;
}
