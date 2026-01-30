using PackageSmith.Core.Configuration;
using PackageSmith.Core.AssemblyDefinition;
using PackageSmith.Core.Dependencies;
using PackageSmith.Core.Templates;

namespace PackageSmith.Core.Generation;

public sealed class PackageGenerator : IPackageGenerator
{
    public bool TryGenerate(in PackageTemplate template, in PackageSmithConfig config, out PackageLayout layout)
    {
        layout = default;

        if (!template.IsValid)
        {
            return false;
        }

        var directories = GenerateDirectories(in template);
        var files = GenerateFiles(in template, in config);

        layout = new PackageLayout(directories, files);
        return true;
    }

    private static VirtualDirectory[] GenerateDirectories(in PackageTemplate template)
    {
        var dirs = new List<VirtualDirectory>
        {
            new(Path.Combine(template.OutputPath, template.PackageName))
        };

        var basePath = Path.Combine(template.OutputPath, template.PackageName);

        // Sub-assemblies
        if (template.HasSubAssemblies)
        {
            var subAssemblies = SubAssemblyDefinition.GetStandardSubAssemblies(template.PackageName);
            foreach (var sub in subAssemblies)
            {
                if (template.SubAssemblies.HasFlag(sub.Type))
                {
                    dirs.Add(new(Path.Combine(basePath, "Runtime", sub.GetFolderName())));
                }
            }
        }
        else
        {
            if (template.SelectedModules.HasFlag(PackageModule.Runtime))
            {
                dirs.Add(new(Path.Combine(basePath, "Runtime")));
            }
        }

        if (template.SelectedModules.HasFlag(PackageModule.Editor))
        {
            dirs.Add(new(Path.Combine(basePath, "Editor")));
        }

        if (template.SelectedModules.HasFlag(PackageModule.Tests))
        {
            dirs.Add(new(Path.Combine(basePath, "Tests")));
        }

        if (template.SelectedModules.HasFlag(PackageModule.Samples))
        {
            dirs.Add(new(Path.Combine(basePath, "Samples~")));
        }

        // Documentation~ folder for DocFX
        dirs.Add(new(Path.Combine(basePath, "Documentation~")));

        return dirs.ToArray();
    }

    private static VirtualFile[] GenerateFiles(in PackageTemplate template, in PackageSmithConfig config)
    {
        var files = new List<VirtualFile>();
        var basePath = Path.Combine(template.OutputPath, template.PackageName);
        var packageName = template.PackageName;
        var displayName = template.DisplayName;
        var description = template.Description;
        var hasSubAssemblies = template.HasSubAssemblies;
        var subAssemblies = template.SubAssemblies;
        var selectedModules = template.SelectedModules;
        var ecsPreset = template.EcsPreset;
        var dependencies = template.Dependencies;

        // package.json
        var manifest = new PackageManifest(
            packageName,
            displayName,
            description,
            config.DefaultUnityVersion,
            config.CompanyName,
            dependencies: dependencies
        );
        files.Add(new VirtualFile(Path.Combine(basePath, "package.json"), manifest.ToJson()));

        // README.md
        files.Add(new VirtualFile(Path.Combine(basePath, "README.md"), MarkdownTemplates.Readme(in template)));

        // LICENSE.md
        files.Add(new VirtualFile(Path.Combine(basePath, "LICENSE.md"), MarkdownTemplates.License(config.CompanyName, DateTime.UtcNow.Year)));

        // CHANGELOG.md
        files.Add(new VirtualFile(Path.Combine(basePath, "CHANGELOG.md"), MarkdownTemplates.Changelog(packageName)));

        // Runtime asmdef(s)
        if (hasSubAssemblies)
        {
            var subList = SubAssemblyDefinition.GetStandardSubAssemblies(packageName);
            foreach (var sub in subList)
            {
                if (subAssemblies.HasFlag(sub.Type))
                {
                    var asmdef = AsmDefTemplate.SubAssembly(packageName, in sub, in ecsPreset);
                    var folderPath = Path.Combine(basePath, "Runtime", sub.GetFolderName());
                    files.Add(new VirtualFile(Path.Combine(folderPath, $"{sub.Name}.asmdef"), asmdef.ToJson()));

                    // Add a starter script with proper namespace
                    var ns = NamespaceGenerator.FromPackageName(packageName, sub.GetFolderName());
                    var folderName = sub.GetFolderName();
                    var scriptContent = $$"""
                    namespace {{ns}};

                    /// <summary>{{folderName}} logic for {{displayName}}</summary>
                    public class {{folderName}}Manager
                    {
                        // TODO: Implement {{folderName}} functionality
                    }
                    """;
                    files.Add(new VirtualFile(Path.Combine(folderPath, $"{sub.GetFolderName()}Manager.cs"), scriptContent));
                }
            }
        }
        else if (selectedModules.HasFlag(PackageModule.Runtime))
        {
            var asmdef = AsmDefTemplate.Runtime(packageName, in ecsPreset);
            files.Add(new VirtualFile(
                Path.Combine(basePath, "Runtime", $"{packageName.Replace('.', '_')}.asmdef"),
                asmdef.ToJson()
            ));
        }

        // Editor asmdef
        if (selectedModules.HasFlag(PackageModule.Editor))
        {
            var runtimeRefs = hasSubAssemblies
                ? SubAssemblyDefinition.GetStandardSubAssemblies(packageName)
                    .Where(s => subAssemblies.HasFlag(s.Type))
                    .Select(s => s.Name)
                    .ToArray()
                : Array.Empty<string>();

            var asmdef = AsmDefTemplate.Editor(packageName, runtimeRefs);
            files.Add(new VirtualFile(
                Path.Combine(basePath, "Editor", $"{packageName.Replace('.', '_')}.Editor.asmdef"),
                asmdef.ToJson()
            ));
        }

        // Tests asmdef
        if (selectedModules.HasFlag(PackageModule.Tests))
        {
            var runtimeRefs = hasSubAssemblies
                ? SubAssemblyDefinition.GetStandardSubAssemblies(packageName)
                    .Where(s => subAssemblies.HasFlag(s.Type))
                    .Select(s => s.Name)
                    .ToArray()
                : Array.Empty<string>();

            var asmdef = AsmDefTemplate.Tests(packageName, runtimeRefs);
            files.Add(new VirtualFile(
                Path.Combine(basePath, "Tests", $"{packageName.Replace('.', '_')}.Tests.asmdef"),
                asmdef.ToJson()
            ));

            // Add TestRunner script
            var testNs = NamespaceGenerator.FromPackageName(packageName, "Tests");
            var testContent = CodeTemplate.TestRunner(testNs, $"{displayName}Tests");
            files.Add(new VirtualFile(
                Path.Combine(basePath, "Tests", $"{displayName}Tests.cs"),
                testContent
            ));
        }

        // packages.config for NuGet dependencies
        if (NuGetConfig.RequiresNuGetForUnity(dependencies))
        {
            var packagesConfig = NuGetConfig.GeneratePackagesConfig(dependencies);
            files.Add(new VirtualFile(Path.Combine(basePath, "packages.config"), packagesConfig));
        }

        // Template files
        GenerateTemplateFiles(template, files, basePath);

        // DocFX documentation stub
        var docFiles = DocFxGenerator.GenerateFiles(packageName, displayName);
        foreach (var docFile in docFiles)
        {
            files.Add(new VirtualFile(Path.Combine(basePath, docFile.Path), docFile.Content));
        }

        return files.ToArray();
    }

    private static void GenerateTemplateFiles(in PackageTemplate template, List<VirtualFile> files, string basePath)
    {
        if (!template.HasTemplate) return;

        var packageName = template.PackageName;
        var displayName = template.DisplayName;
        var ns = NamespaceGenerator.FromPackageName(packageName);

        if (template.SelectedTemplate.HasFlag(TemplateType.MonoBehaviour))
        {
            var content = CodeTemplate.MonoBehaviour(ns, displayName);
            files.Add(new VirtualFile(
                Path.Combine(basePath, "Runtime", $"{displayName}.cs"),
                content
            ));
        }

        if (template.SelectedTemplate.HasFlag(TemplateType.ScriptableObject))
        {
            var content = CodeTemplate.ScriptableObject(ns, $"{displayName}Config");
            files.Add(new VirtualFile(
                Path.Combine(basePath, "Runtime", $"{displayName}Config.cs"),
                content
            ));
        }

        if (template.SelectedTemplate.HasFlag(TemplateType.SystemBase))
        {
            var content = CodeTemplate.SystemBase(ns, $"{displayName}System");
            files.Add(new VirtualFile(
                Path.Combine(basePath, "Runtime", $"{displayName}System.cs"),
                content
            ));
        }

        if (template.SelectedTemplate.HasFlag(TemplateType.IComponentData))
        {
            var content = CodeTemplate.IComponentData(ns, $"{displayName}Component");
            files.Add(new VirtualFile(
                Path.Combine(basePath, "Runtime", $"{displayName}Component.cs"),
                content
            ));
        }

        if (template.SelectedTemplate.HasFlag(TemplateType.ISharedComponentData))
        {
            var content = CodeTemplate.ISharedComponentData(ns, $"{displayName}SharedComponent");
            files.Add(new VirtualFile(
                Path.Combine(basePath, "Runtime", $"{displayName}SharedComponent.cs"),
                content
            ));
        }

        if (template.SelectedTemplate.HasFlag(TemplateType.Authoring))
        {
            var content = CodeTemplate.ScaffoldAuthoring(ns, displayName, $"{displayName}Component");
            files.Add(new VirtualFile(
                Path.Combine(basePath, "Authoring", $"{displayName}Authoring.cs"),
                content
            ));
        }

        if (template.SelectedTemplate.HasFlag(TemplateType.EcsFull))
        {
            var featureName = displayName;
            var componentName = $"{featureName}Component";

            // Component Data
            var componentData = CodeTemplate.IComponentData(ns, componentName);
            files.Add(new VirtualFile(
                Path.Combine(basePath, "Runtime/Data", $"{componentName}.cs"),
                componentData
            ));

            // Authoring in Authoring sub-assembly if using sub-assemblies
            if (template.HasSubAssemblies)
            {
                var authoringNs = NamespaceGenerator.FromPackageName(packageName, "Authoring");
                var authoringContent = CodeTemplate.ScaffoldAuthoring(authoringNs, featureName, componentName);
                files.Add(new VirtualFile(
                    Path.Combine(basePath, "Runtime/Authoring", $"{featureName}Authoring.cs"),
                    authoringContent
                ));
            }

            // System
            var systemContent = CodeTemplate.SystemBase(ns, $"{featureName}System");
            files.Add(new VirtualFile(
                Path.Combine(basePath, "Runtime/Systems", $"{featureName}System.cs"),
                systemContent
            ));
        }
    }
}
