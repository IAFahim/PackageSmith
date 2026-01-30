using PackageSmith.Core.AssemblyDefinition;
using PackageSmith.Core.Configuration;

namespace PackageSmith.Core.Generation;

public static class AsmDefTemplate
{
    public static AsmDefFile Runtime(string packageName, in EcsPreset ecsPreset)
    {
        var asmdefName = packageName.Replace('.', '_');
        var refs = new List<AsmDefReference>();

        if (ecsPreset.IsEnabled)
        {
            refs.AddRange(ecsPreset.GetRuntimeReferences());
        }

        return new AsmDefFile(
            name: asmdefName,
            references: refs.ToArray(),
            allowUnsafeCode: ecsPreset.EnableBurst,
            defineConstraints: ecsPreset.GetDefineConstraints()
        );
    }

    public static AsmDefFile Editor(string packageName, string[] runtimeReferences)
    {
        var asmdefName = $"{packageName.Replace('.', '_')}.Editor";
        var refs = new List<AsmDefReference>
        {
            AsmDefReference.Custom(packageName.Replace('.', '_'))
        };

        foreach (var refName in runtimeReferences)
        {
            refs.Add(AsmDefReference.Custom(refName));
        }

        return new AsmDefFile(
            name: asmdefName,
            references: refs.ToArray(),
            includePlatforms: new[] { AsmDefPlatform.Editor },
            defineConstraints: new[] { "UNITY_EDITOR" }
        );
    }

    public static AsmDefFile Tests(string packageName, string[] runtimeReferences)
    {
        var asmdefName = $"{packageName.Replace('.', '_')}.Tests";
        var refs = new List<AsmDefReference>
        {
            AsmDefReference.Unity("UnityEngine.TestRunner"),
            AsmDefReference.Unity("UnityEditor.TestRunner"),
            AsmDefReference.Custom(packageName.Replace('.', '_'))
        };

        foreach (var refName in runtimeReferences)
        {
            refs.Add(AsmDefReference.Custom(refName));
        }

        return new AsmDefFile(
            name: asmdefName,
            references: refs.ToArray(),
            autoReferenced: false,
            overrideReferences: true,
            defineConstraints: new[] { "UNITY_INCLUDE_TESTS" }
        );
    }

    public static AsmDefFile SubAssembly(
        string packageName,
        in SubAssemblyDefinition subAssembly,
        in EcsPreset ecsPreset)
    {
        var refs = new List<AsmDefReference>();

        // Add dependencies
        foreach (var dep in subAssembly.Dependencies)
        {
            refs.Add(AsmDefReference.Custom(dep));
        }

        // Add ECS references
        if (ecsPreset.IsEnabled)
        {
            refs.AddRange(ecsPreset.GetRuntimeReferences());
        }

        return new AsmDefFile(
            name: subAssembly.Name,
            references: refs.ToArray(),
            allowUnsafeCode: ecsPreset.EnableBurst,
            defineConstraints: ecsPreset.GetDefineConstraints()
        );
    }
}
