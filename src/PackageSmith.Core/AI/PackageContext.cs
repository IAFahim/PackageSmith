using System.Text.Json;
using PackageSmith.Core.Configuration;
using PackageSmith.Core.Generation;
using PackageSmith.Core.AssemblyDefinition;

namespace PackageSmith.Core.AI;

public readonly struct PackageContext
{
    public readonly string PackageName;
    public readonly string DisplayName;
    public readonly string Description;
    public readonly string UnityVersion;
    public readonly string CompanyName;
    public readonly string[] Modules;
    public readonly string[] SubAssemblies;
    public readonly bool HasECS;
    public readonly string[] Dependencies;
    public readonly string ArchitectureDescription;

    public PackageContext(
        string packageName,
        string displayName,
        string description,
        string unityVersion,
        string companyName,
        string[] modules,
        string[] subAssemblies,
        bool hasECS,
        string[] dependencies,
        string architectureDescription)
    {
        PackageName = packageName;
        DisplayName = displayName;
        Description = description;
        UnityVersion = unityVersion;
        CompanyName = companyName;
        Modules = modules;
        SubAssemblies = subAssemblies;
        HasECS = hasECS;
        Dependencies = dependencies;
        ArchitectureDescription = architectureDescription;
    }

    public readonly string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public readonly string ToMarkdown()
    {
        var modules = string.Join(", ", Modules);
        var subAsms = string.Join(", ", SubAssemblies);
        var deps = Dependencies.Length > 0 ? string.Join(", ", Dependencies) : "None";
        var ecs = HasECS ? "Yes (Entities, Burst, Collections, Mathematics)" : "No";

        return $$"""
        # {{PackageName}} - Package Context

        **Display Name:** {{DisplayName}}
        **Description:** {{Description}}
        **Company:** {{CompanyName}}
        **Unity Version:** {{UnityVersion}}

        ## Architecture

        This package uses the following structure:

        - **Modules:** {{modules}}
        - **Sub-Assemblies:** {{subAsms}}
        - **ECS Enabled:** {{ecs}}
        - **Dependencies:** {{deps}}

        {{ArchitectureDescription}}

        ## AI Modification Guidelines

        When modifying this package, follow these rules:

        1. **Namespace Convention:** All code should use the package name as base namespace
        2. **Assembly References:** Sub-assemblies have specific dependencies (Core <- Data <- Authoring/Runtime <- Systems)
        3. **ECS Components:** Add IComponentData structs in the Data sub-assembly
        4. **Systems:** Add ISystem implementations in the Systems sub-assembly
        5. **Authoring:** Add MonoBehaviour + Baker in the Authoring sub-assembly for ECS components

        ## File Structure

        ```
        {{PackageName}}/
        ├── Runtime/
        │   ├── Core/          (Core types and interfaces)
        │   ├── Data/          (IComponentData, ISharedComponentData)
        │   ├── Authoring/     (MonoBehaviour + Baker for ECS)
        │   ├── Runtime/       (Runtime utilities)
        │   └── Systems/       (ISystem implementations)
        ├── Editor/            (Editor-only tools)
        ├── Tests/             (NUnit test fixtures)
        ├── Samples~/          (Example scenes and usage)
        └── package.json
        ```
        """;
    }

    public static PackageContext FromTemplate(in PackageTemplate template, in PackageSmithConfig config)
    {
        var modules = GetModuleNames(template.SelectedModules);
        var subAssemblies = template.HasSubAssemblies
            ? new[] { "Core", "Data", "Authoring", "Runtime", "Systems" }
            : Array.Empty<string>();

        var deps = template.Dependencies.Select(d => d.Name).ToArray();

        var architecture = BuildArchitectureDescription(template);

        return new PackageContext(
            template.PackageName,
            template.DisplayName,
            template.Description,
            template.UnityVersion,
            template.CompanyName,
            modules,
            subAssemblies,
            template.IsEcsEnabled,
            deps,
            architecture
        );
    }

    private static string BuildArchitectureDescription(in PackageTemplate template)
    {
        var description = "### Architecture Overview\n\n";

        if (template.HasSubAssemblies)
        {
            description += "This package uses a sub-assembly structure for better code organization:\n\n";
            description += "- **Core:** Base types and interfaces shared across all assemblies\n";
            description += "- **Data:** ECS component data (IComponentData, ISharedComponentData)\n";
            description += "- **Authoring:** MonoBehaviour components and Bakers for authoring ECS entities\n";
            description += "- **Runtime:** Runtime utilities and systems\n";
            description += "- **Systems:** ECS systems (ISystem implementations)\n\n";
            description += "Dependency Chain: Core → Data → Authoring/Runtime → Systems\n";
        }

        if (template.IsEcsEnabled)
        {
            description += "\n### ECS Configuration\n\n";
            description += "This package uses Unity's high-performance DOTS architecture:\n";
            description += "- **Unity.Entities:** Core ECS framework\n";
            description += "- **Unity.Burst:** Burst compiler for high-performance C#\n";
            description += "- **Unity.Collections:** Container types optimized for ECS\n";
            description += "- **Unity.Mathematics:** Vector math library (SIMD optimized)\n";
            description += "- **Unity.Jobs:** Job system for parallel execution\n";
        }

        return description;
    }

    private static string[] GetModuleNames(PackageModule modules)
    {
        var list = new List<string>();
        foreach (PackageModule value in Enum.GetValues(typeof(PackageModule)))
        {
            if (value != PackageModule.None && modules.HasFlag(value))
            {
                list.Add(value.ToString());
            }
        }
        return list.ToArray();
    }
}
