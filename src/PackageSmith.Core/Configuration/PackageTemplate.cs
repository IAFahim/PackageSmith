using PackageSmith.Core.AssemblyDefinition;
using PackageSmith.Core.Dependencies;
using PackageSmith.Core.Templates;

namespace PackageSmith.Core.Configuration;

[Serializable]
public struct PackageTemplate
{
    public string PackageName;
    public string DisplayName;
    public string Description;
    public PackageModule SelectedModules;
    public string OutputPath;
    public string CompanyName;
    public string UnityVersion;

    public EcsPreset EcsPreset;
    public SubAssemblyType SubAssemblies;
    public bool EnableSubAssemblies;

    public PackageDependency[] Dependencies;

    public TemplateType SelectedTemplate;
    public string? CustomTemplateName;

    public readonly bool IsValid => !string.IsNullOrWhiteSpace(PackageName);
    public readonly bool IsEcsEnabled => EcsPreset.IsEnabled;
    public readonly bool HasSubAssemblies => EnableSubAssemblies && SubAssemblies != SubAssemblyType.None;
    public readonly bool HasDependencies => Dependencies.Length > 0;
    public readonly bool HasTemplate => SelectedTemplate != TemplateType.None;
}
