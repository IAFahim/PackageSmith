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
        var asmdefRoot = NamespaceGenerator.GetAsmDefRootFromPackageName(template.PackageName);

        // Sub-assemblies - at root level (not nested under Runtime/)
        if (template.HasSubAssemblies)
        {
            var subAssemblies = SubAssemblyDefinition.GetStandardSubAssemblies(asmdefRoot);
            foreach (var sub in subAssemblies)
            {
                if (template.SubAssemblies.HasFlag(sub.Type))
                {
                    // Use assembly name as folder name (e.g., "Timeline.Core", "Timeline.Data")
                    dirs.Add(new(Path.Combine(basePath, sub.Name)));
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

        // Editor/Tests folder names depend on sub-assembly mode
        if (template.SelectedModules.HasFlag(PackageModule.Editor))
        {
            var editorFolder = template.HasSubAssemblies ? $"{asmdefRoot}.Editor" : "Editor";
            dirs.Add(new(Path.Combine(basePath, editorFolder)));
        }

        if (template.SelectedModules.HasFlag(PackageModule.Tests))
        {
            var testsFolder = template.HasSubAssemblies ? $"{asmdefRoot}.Tests" : "Tests";
            dirs.Add(new(Path.Combine(basePath, testsFolder)));
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
        
        // Extract asmdef root name from package name (e.g., "com.company.intent" -> "Intent")
        var asmdefRoot = NamespaceGenerator.GetAsmDefRootFromPackageName(packageName);

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

        // Runtime asmdef(s) - sub-assemblies at root level
        if (hasSubAssemblies)
        {
            var subList = SubAssemblyDefinition.GetStandardSubAssemblies(asmdefRoot);
            foreach (var sub in subList)
            {
                if (subAssemblies.HasFlag(sub.Type))
                {
                    var asmdef = AsmDefTemplate.SubAssembly(asmdefRoot, in sub, in ecsPreset);
                    // Use assembly name as folder path (e.g., "Timeline.Core", "Timeline.Data")
                    var folderPath = Path.Combine(basePath, sub.Name);
                    files.Add(new VirtualFile(Path.Combine(folderPath, $"{sub.Name}.asmdef"), asmdef.ToJson()));

                    // Add AssemblyInfo.cs for each sub-assembly
                    var assemblyInfoContent = $$"""
                    using System.Reflection;
                    using System.Runtime.InteropServices;

                    [assembly: AssemblyTitle("{{sub.Name}}")]
                    [assembly: AssemblyProduct("{{displayName}}")]
                    [assembly: AssemblyCompany("{{config.CompanyName}}")]
                    [assembly: AssemblyVersion("1.0.0.0")]
                    [assembly: AssemblyFileVersion("1.0.0.0")]
                    """;
                    files.Add(new VirtualFile(Path.Combine(folderPath, "AssemblyInfo.cs"), assemblyInfoContent));
                }
            }
        }
        else if (selectedModules.HasFlag(PackageModule.Runtime))
        {
            var asmdef = AsmDefTemplate.Runtime(asmdefRoot, in ecsPreset);
            files.Add(new VirtualFile(
                Path.Combine(basePath, "Runtime", $"{asmdefRoot}.asmdef"),
                asmdef.ToJson()
            ));
        }

        // Editor asmdef
        if (selectedModules.HasFlag(PackageModule.Editor))
        {
            var editorAsmdefName = hasSubAssemblies ? $"{asmdefRoot}.Editor" : $"{asmdefRoot}.Editor";
            var runtimeRefs = hasSubAssemblies
                ? SubAssemblyDefinition.GetStandardSubAssemblies(asmdefRoot)
                    .Where(s => subAssemblies.HasFlag(s.Type))
                    .Select(s => s.Name)
                    .ToArray()
                : Array.Empty<string>();

            var asmdef = AsmDefTemplate.Editor(asmdefRoot, runtimeRefs);
            var editorFolder = hasSubAssemblies ? editorAsmdefName : "Editor";
            files.Add(new VirtualFile(
                Path.Combine(basePath, editorFolder, $"{editorAsmdefName}.asmdef"),
                asmdef.ToJson()
            ));

            // Add AssemblyInfo.cs for Editor
            var editorAssemblyInfoContent = $$"""
            using System.Reflection;
            using System.Runtime.InteropServices;

            [assembly: AssemblyTitle("{{editorAsmdefName}}")]
            [assembly: AssemblyProduct("{{displayName}}")]
            [assembly: AssemblyCompany("{{config.CompanyName}}")]
            [assembly: AssemblyVersion("1.0.0.0")]
            [assembly: AssemblyFileVersion("1.0.0.0")]
            """;
            files.Add(new VirtualFile(Path.Combine(basePath, editorFolder, "AssemblyInfo.cs"), editorAssemblyInfoContent));
        }

        // Tests asmdef
        if (selectedModules.HasFlag(PackageModule.Tests))
        {
            var testsAsmdefName = hasSubAssemblies ? $"{asmdefRoot}.Tests" : $"{asmdefRoot}.Tests";
            var runtimeRefs = hasSubAssemblies
                ? SubAssemblyDefinition.GetStandardSubAssemblies(asmdefRoot)
                    .Where(s => subAssemblies.HasFlag(s.Type))
                    .Select(s => s.Name)
                    .ToArray()
                : Array.Empty<string>();

            var asmdef = AsmDefTemplate.Tests(asmdefRoot, runtimeRefs);
            var testsFolder = hasSubAssemblies ? testsAsmdefName : "Tests";
            files.Add(new VirtualFile(
                Path.Combine(basePath, testsFolder, $"{testsAsmdefName}.asmdef"),
                asmdef.ToJson()
            ));

            // Add AssemblyInfo.cs for Tests
            var assemblyInfoContent = $$"""
            using System.Reflection;
            using System.Runtime.InteropServices;

            [assembly: AssemblyTitle("{{testsAsmdefName}}")]
            [assembly: AssemblyProduct("{{displayName}}")]
            [assembly: AssemblyCompany("{{config.CompanyName}}")]
            [assembly: AssemblyVersion("1.0.0.0")]
            [assembly: AssemblyFileVersion("1.0.0.0")]
            """;
            files.Add(new VirtualFile(Path.Combine(basePath, testsFolder, "AssemblyInfo.cs"), assemblyInfoContent));
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
        var asmdefRoot = NamespaceGenerator.GetAsmDefRootFromPackageName(packageName);
        var ns = NamespaceGenerator.FromPackageName(packageName);
        var hasSubAssemblies = template.HasSubAssemblies;

        // For sub-assemblies, use the assembly name as folder path
        var dataFolder = hasSubAssemblies ? $"{asmdefRoot}.Data" : "Runtime";
        var authoringFolder = hasSubAssemblies ? $"{asmdefRoot}.Authoring" : "Authoring";
        var systemsFolder = hasSubAssemblies ? $"{asmdefRoot}.Systems" : "Runtime";
        var runtimeFolder = "Runtime"; // Non-sub-assembly packages still use Runtime/

        if (template.SelectedTemplate.HasFlag(TemplateType.MonoBehaviour))
        {
            var content = CodeTemplate.MonoBehaviour(ns, displayName);
            files.Add(new VirtualFile(
                Path.Combine(basePath, runtimeFolder, $"{displayName}.cs"),
                content
            ));
        }

        if (template.SelectedTemplate.HasFlag(TemplateType.ScriptableObject))
        {
            var content = CodeTemplate.ScriptableObject(ns, $"{displayName}Config");
            files.Add(new VirtualFile(
                Path.Combine(basePath, runtimeFolder, $"{displayName}Config.cs"),
                content
            ));
        }

        if (template.SelectedTemplate.HasFlag(TemplateType.SystemBase))
        {
            var content = CodeTemplate.SystemBase(ns, $"{displayName}System");
            files.Add(new VirtualFile(
                Path.Combine(basePath, systemsFolder, $"{displayName}System.cs"),
                content
            ));
        }

        if (template.SelectedTemplate.HasFlag(TemplateType.IComponentData))
        {
            var content = CodeTemplate.IComponentData(ns, $"{displayName}Component");
            files.Add(new VirtualFile(
                Path.Combine(basePath, dataFolder, $"{displayName}Component.cs"),
                content
            ));
        }

        if (template.SelectedTemplate.HasFlag(TemplateType.ISharedComponentData))
        {
            var content = CodeTemplate.ISharedComponentData(ns, $"{displayName}SharedComponent");
            files.Add(new VirtualFile(
                Path.Combine(basePath, dataFolder, $"{displayName}SharedComponent.cs"),
                content
            ));
        }

        if (template.SelectedTemplate.HasFlag(TemplateType.Authoring))
        {
            var content = CodeTemplate.ScaffoldAuthoring(ns, displayName, $"{displayName}Component");
            files.Add(new VirtualFile(
                Path.Combine(basePath, authoringFolder, $"{displayName}Authoring.cs"),
                content
            ));
        }

        if (template.SelectedTemplate.HasFlag(TemplateType.EcsFull))
        {
            var featureName = displayName;
            var componentName = $"{featureName}Component";

            // Component Data - goes to Data assembly
            var componentData = CodeTemplate.IComponentData(ns, componentName);
            files.Add(new VirtualFile(
                Path.Combine(basePath, dataFolder, $"{componentName}.cs"),
                componentData
            ));

            // Authoring in Authoring sub-assembly
            if (hasSubAssemblies)
            {
                var authoringNs = NamespaceGenerator.FromPackageName(packageName, "Authoring");
                var authoringContent = CodeTemplate.ScaffoldAuthoring(authoringNs, featureName, componentName);
                files.Add(new VirtualFile(
                    Path.Combine(basePath, authoringFolder, $"{featureName}Authoring.cs"),
                    authoringContent
                ));
            }

            // System - goes to Systems assembly
            var systemContent = CodeTemplate.SystemBase(ns, $"{featureName}System");
            files.Add(new VirtualFile(
                Path.Combine(basePath, systemsFolder, $"{featureName}System.cs"),
                systemContent
            ));
        }
    }
}
