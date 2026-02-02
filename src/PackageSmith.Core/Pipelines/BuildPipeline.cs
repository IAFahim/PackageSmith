using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PackageSmith.Core.Extensions;
using PackageSmith.Core.Interfaces;
using PackageSmith.Core.Logic;
using PackageSmith.Data.Config;
using PackageSmith.Data.State;
using PackageSmith.Data.Types;

namespace PackageSmith.Core.Pipelines;

public sealed class BuildPipeline : IPackageGenerator
{
    public bool TryGenerate(in PackageState package, in AppConfig config, out PackageLayoutState layout,
        out VirtualFileState[] files)
    {
        layout = default;
        files = Array.Empty<VirtualFileState>();

        package.TryValidate(out var isValid);
        if (!isValid) return false;

        var hasSubAssemblies = package.HasSubAssemblies();
        var directories = GenerateDirectories(in package, hasSubAssemblies);
        files = GenerateFiles(in package, in config, hasSubAssemblies);

        layout = new PackageLayoutState
        {
            DirectoryCount = directories.Length,
            FileCount = files.Length
        };

        return true;
    }

    private static VirtualDirectoryState[] GenerateDirectories(in PackageState package, bool hasSubAssemblies)
    {
        var dirs = new List<VirtualDirectoryState>();

        package.TryGetBasePath(out var basePath);
        dirs.Add(new VirtualDirectoryState { Path = basePath });

        package.TryGetAsmDefRoot(out var asmdefRoot);

        if (hasSubAssemblies)
        {
            AsmDefLogic.GetStandardSubAssemblies(package.PackageName, out var subAssemblies);
            foreach (var sub in subAssemblies)
                if (package.SubAssemblies.HasFlag(sub.Type))
                    dirs.Add(new VirtualDirectoryState { Path = Path.Combine(basePath, sub.Name) });
        }
        else if (package.HasModule(PackageModuleType.Runtime))
        {
            dirs.Add(new VirtualDirectoryState { Path = Path.Combine(basePath, "Runtime") });
        }

        if (package.HasModule(PackageModuleType.Editor))
        {
            var editorFolder = hasSubAssemblies ? $"{asmdefRoot}.Editor" : "Editor";
            dirs.Add(new VirtualDirectoryState { Path = Path.Combine(basePath, editorFolder) });
        }

        if (package.HasModule(PackageModuleType.Tests))
        {
            var testsRoot = hasSubAssemblies ? $"{asmdefRoot}.Tests" : "Tests";
            dirs.Add(new VirtualDirectoryState { Path = Path.Combine(basePath, testsRoot) });
            dirs.Add(new VirtualDirectoryState { Path = Path.Combine(basePath, testsRoot, "Editor") });
            dirs.Add(new VirtualDirectoryState { Path = Path.Combine(basePath, testsRoot, "Runtime") });
        }

        if (package.HasModule(PackageModuleType.Samples))
        {
            var samplesPath = Path.Combine(basePath, "Samples~");
            dirs.Add(new VirtualDirectoryState { Path = samplesPath });
            dirs.Add(new VirtualDirectoryState { Path = Path.Combine(samplesPath, "Basic") });
            dirs.Add(new VirtualDirectoryState { Path = Path.Combine(samplesPath, "Advanced") });

            if (package.EcsPreset.EnableEntities || package.EcsPreset.EnableBurst)
                dirs.Add(new VirtualDirectoryState { Path = Path.Combine(samplesPath, "ECS") });
        }

        dirs.Add(new VirtualDirectoryState { Path = Path.Combine(basePath, "Documentation~") });

        return dirs.ToArray();
    }

    private static VirtualFileState[] GenerateFiles(in PackageState package, in AppConfig config, bool hasSubAssemblies)
    {
        var files = new List<VirtualFileState>();

        package.TryGetBasePath(out var basePath);
        package.TryGetAsmDefRoot(out var asmdefRoot);

        files.Add(new VirtualFileState
        {
            Path = Path.Combine(basePath, "package.json"),
            Content = GeneratePackageManifest(in package, in config)
        });

        files.Add(new VirtualFileState
        {
            Path = Path.Combine(basePath, "README.md"),
            Content = GenerateReadme(in package)
        });

        files.Add(new VirtualFileState
        {
            Path = Path.Combine(basePath, "Documentation~", $"{package.PackageName}.md"),
            Content = $"# {package.DisplayName} Documentation\n\nDetailed documentation for {package.PackageName}."
        });

        files.Add(new VirtualFileState
        {
            Path = Path.Combine(basePath, ".gitignore"),
            Content = GitLogic.GenerateGitIgnore()
        });

        if (package.License != LicenseType.None)
            files.Add(new VirtualFileState
            {
                Path = Path.Combine(basePath, "LICENSE.md"),
                Content = GitLogic.GenerateLicense(package.License, DateTime.Now.Year.ToString(), package.CompanyName)
            });

        files.Add(new VirtualFileState
        {
            Path = Path.Combine(basePath, "CHANGELOG.md"),
            Content = GitLogic.GenerateChangelog(package.PackageName)
        });

        files.Add(new VirtualFileState
        {
            Path = Path.Combine(basePath, "Third Party Notices.md"),
            Content = GenerateThirdPartyNotices(in package)
        });

        if (package.HasModule(PackageModuleType.Samples))
            files.Add(new VirtualFileState
            {
                Path = Path.Combine(basePath, "Samples~", "Basic", "SampleScript.cs"),
                Content = "// Basic sample script\npublic class SampleScript {}\n"
            });

        if (package.HasModule(PackageModuleType.Runtime) && !hasSubAssemblies)
        {
            AsmDefLogic.GetEcsReferences(in package.EcsPreset, out var refs);
            var refNames = refs.Select(r => r.Name).ToArray();
            var defines = AsmDefGenerationLogic.GenerateVersionDefines(refNames, package.PackageName);
            var asmdefContent =
                AsmDefGenerationLogic.GenerateJson(asmdefRoot, refs, package.EcsPreset.EnableBurst, defines);

            files.Add(new VirtualFileState
            {
                Path = Path.Combine(basePath, "Runtime", $"{asmdefRoot}.asmdef"),
                Content = asmdefContent
            });
        }

        if (package.HasModule(PackageModuleType.Editor))
        {
            var editorAsmdefName = hasSubAssemblies ? $"{asmdefRoot}.Editor" : $"{asmdefRoot}.Editor";
            var runtimeRefs = hasSubAssemblies
                ? SubAssemblyLogic.GetRuntimeAssemblies(package.SubAssemblies, asmdefRoot)
                : new[] { asmdefRoot };

            var defines = AsmDefGenerationLogic.GenerateVersionDefines(runtimeRefs, package.PackageName);
            var editorAsmdef = AsmDefGenerationLogic.GenerateEditorJson(editorAsmdefName, runtimeRefs, defines);
            var editorFolder = hasSubAssemblies ? editorAsmdefName : "Editor";
            files.Add(new VirtualFileState
            {
                Path = Path.Combine(basePath, editorFolder, $"{editorAsmdefName}.asmdef"),
                Content = editorAsmdef
            });
        }

        if (package.HasModule(PackageModuleType.Tests))
        {
            var testsRoot = hasSubAssemblies ? $"{asmdefRoot}.Tests" : "Tests";
            var runtimeAsm = asmdefRoot;
            var editorAsm = $"{asmdefRoot}.Editor";

            var runtimeTestsName = $"{asmdefRoot}.Tests";
            var runtimeTestsRefs = new[] { runtimeAsm };
            var runtimeTestsDefines =
                AsmDefGenerationLogic.GenerateVersionDefines(runtimeTestsRefs, package.PackageName);
            var runtimeTestsJson = AsmDefGenerationLogic.GenerateTestsJson(runtimeTestsName, runtimeTestsRefs,
                Array.Empty<string>(), Array.Empty<string>(), runtimeTestsDefines);

            files.Add(new VirtualFileState
            {
                Path = Path.Combine(basePath, testsRoot, "Runtime", $"{runtimeTestsName}.asmdef"),
                Content = runtimeTestsJson
            });

            if (package.HasModule(PackageModuleType.Editor))
            {
                var editorTestsName = $"{asmdefRoot}.Editor.Tests";
                var editorTestsRefs = new[] { runtimeAsm, editorAsm };
                var editorTestsDefines =
                    AsmDefGenerationLogic.GenerateVersionDefines(editorTestsRefs, package.PackageName);
                var editorTestsJson = AsmDefGenerationLogic.GenerateTestsJson(editorTestsName, new[] { runtimeAsm },
                    new[] { editorAsm }, new[] { "Editor" }, editorTestsDefines);

                files.Add(new VirtualFileState
                {
                    Path = Path.Combine(basePath, testsRoot, "Editor", $"{editorTestsName}.asmdef"),
                    Content = editorTestsJson
                });
            }
        }

        package.TryGetNamespace(out var ns);
        var featureName = package.DisplayName;
        var packageParts = package.PackageName.Split('.');
        var baseName = packageParts.Length > 0 ? packageParts[^1] : "Feature";

        if (package.SelectedTemplate.HasFlag(TemplateType.MonoBehaviour))
        {
            var code = TemplateLogic.GenerateMonoBehaviour(ns, $"{featureName}Behavior");
            var runtimeFolder = hasSubAssemblies ? $"{asmdefRoot}.Runtime" : "Runtime";
            files.Add(new VirtualFileState
            {
                Path = Path.Combine(basePath, runtimeFolder, $"{featureName}Behavior.cs"),
                Content = code
            });
        }

        if (package.SelectedTemplate.HasFlag(TemplateType.ScriptableObject))
        {
            var code = TemplateLogic.GenerateScriptableObject(ns, $"{featureName}Config");
            var runtimeFolder = hasSubAssemblies ? $"{asmdefRoot}.Runtime" : "Runtime";
            files.Add(new VirtualFileState
            {
                Path = Path.Combine(basePath, runtimeFolder, $"{featureName}Config.cs"),
                Content = code
            });
        }

        if (package.SelectedTemplate.HasFlag(TemplateType.SystemBase))
        {
            var code = TemplateLogic.GenerateSystemBase(ns, $"{featureName}System");
            var systemsFolder = hasSubAssemblies ? $"{asmdefRoot}.Systems" : "Runtime";
            files.Add(new VirtualFileState
            {
                Path = Path.Combine(basePath, systemsFolder, $"{featureName}System.cs"),
                Content = code
            });
        }

        if (package.SelectedTemplate.HasFlag(TemplateType.IComponentData))
        {
            var code = TemplateLogic.GenerateIComponentData(ns, $"{baseName}Component");
            var dataFolder = hasSubAssemblies ? $"{asmdefRoot}.Data" : "Runtime";
            files.Add(new VirtualFileState
            {
                Path = Path.Combine(basePath, dataFolder, $"{baseName}Component.cs"),
                Content = code
            });
        }

        if (package.SelectedTemplate.HasFlag(TemplateType.Authoring))
        {
            var code = TemplateLogic.GenerateAuthoring(ns, baseName, $"{baseName}Component");
            var authoringFolder = hasSubAssemblies ? $"{asmdefRoot}.Authoring" : "Authoring";
            files.Add(new VirtualFileState
            {
                Path = Path.Combine(basePath, authoringFolder, $"{baseName}Authoring.cs"),
                Content = code
            });
        }

        if (package.SelectedTemplate.HasFlag(TemplateType.EcsFull))
        {
            var componentName = $"{baseName}Component";

            var componentCode = TemplateLogic.GenerateIComponentData(ns, componentName);
            var dataFolder = hasSubAssemblies ? $"{asmdefRoot}.Data" : "Runtime";
            files.Add(new VirtualFileState
            {
                Path = Path.Combine(basePath, dataFolder, $"{componentName}.cs"),
                Content = componentCode
            });

            if (hasSubAssemblies)
            {
                var authoringNs = $"{ns}.Authoring";
                var authoringCode = TemplateLogic.GenerateAuthoring(authoringNs, baseName, componentName);
                files.Add(new VirtualFileState
                {
                    Path = Path.Combine(basePath, $"{asmdefRoot}.Authoring", $"{baseName}Authoring.cs"),
                    Content = authoringCode
                });
            }

            var systemCode = TemplateLogic.GenerateSystemBase(ns, $"{baseName}System");
            var systemsFolder = hasSubAssemblies ? $"{asmdefRoot}.Systems" : "Runtime";
            files.Add(new VirtualFileState
            {
                Path = Path.Combine(basePath, systemsFolder, $"{baseName}System.cs"),
                Content = systemCode
            });
        }

        if (package.SelectedTemplate.HasFlag(TemplateType.DodFull))
        {
            var code = TemplateLogic.GenerateDodFull(ns, baseName);
            var runtimeFolder = hasSubAssemblies ? $"{asmdefRoot}.Runtime" : "Runtime";
            files.Add(new VirtualFileState
            {
                Path = Path.Combine(basePath, runtimeFolder, $"{baseName}System.cs"),
                Content = code
            });
        }

        return files.ToArray();
    }

    private static string GeneratePackageManifest(in PackageState package, in AppConfig config)
    {
        GitLogic.TryGetGitConfig(out var gitName, out var gitEmail);
        var authorName = string.IsNullOrEmpty(gitName) || gitName == "Unknown" ? config.CompanyName : gitName;
        var authorEmail = gitEmail == "unknown@local" ? "" : gitEmail;

        var authorObj = string.IsNullOrEmpty(authorEmail)
            ? $"{{\"name\": \"{authorName}\"}}"
            : $"{{\"name\": \"{authorName}\", \"email\": \"{authorEmail}\"}}";

        var deps = string.Empty;
        if (package.Dependencies != null && package.Dependencies.Count > 0)
        {
            deps = ",\n\t\"dependencies\": {";
            var first = true;
            foreach (var dep in package.Dependencies)
            {
                if (!first) deps += ",";
                deps += $"\n\t\t\"{dep.Name}\": \"{dep.Version}\"";
                first = false;
            }

            deps += "\n\t}";
        }

        var samples = string.Empty;
        if (package.HasModule(PackageModuleType.Samples))
        {
            var sampleList = new List<string>
            {
                "{\"displayName\": \"Basic Setup\", \"description\": \"Basic usage examples\", \"path\": \"Samples~/Basic\"}",
                "{\"displayName\": \"Advanced Techniques\", \"description\": \"Advanced features and optimization\", \"path\": \"Samples~/Advanced\"}"
            };

            if (package.EcsPreset.EnableEntities || package.EcsPreset.EnableBurst)
                sampleList.Add(
                    "{\"displayName\": \"ECS Examples\", \"description\": \"Data-oriented design samples\", \"path\": \"Samples~/ECS\"}");

            samples = ",\n\t\"samples\": [\n\t\t" + string.Join(",\n\t\t", sampleList) + "\n\t]";
        }

        return $$"""
                 {
                 	"name": "{{package.PackageName}}",
                 	"version": "1.0.0",
                 	"displayName": "{{package.DisplayName}}",
                 	"description": "{{package.Description}}",
                 	"unity": "{{config.DefaultUnityVersion}}",
                 	"author": {{authorObj}},
                 	"documentationUrl": "https://github.com/{{authorName.Replace(" ", "")}}/{{package.PackageName}}"{{deps}}{{samples}}
                 }
                 """;
    }

    private static string GenerateReadme(in PackageState package)
    {
        return $$"""
                 # {{package.DisplayName}}

                 {{package.Description}}

                 ## Package Contents
                 - **Runtime**: Core logic and systems.
                 - **Editor**: Unity Editor extensions and tools.
                 - **Tests**: Automated tests for ensuring stability.
                 - **Samples**: Example implementations and use cases.

                 ## Installation
                 To install this package, use the Unity Package Manager and point to the Git URL or local folder.

                 ## Requirements
                 - Unity {{package.UnityVersion}} or newer.

                 ## Limitations
                 - [List any known limitations here]

                 ## Workflows
                 1. [Step 1]
                 2. [Step 2]

                 ## Reference
                 - [Detailed API or property descriptions]

                 ## Documentation
                 Additional documentation can be found in the `Documentation~` folder.
                 """;
    }

    private static string GenerateThirdPartyNotices(in PackageState package)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Third Party Notices");
        sb.AppendLine();
        sb.AppendLine(
            "This package contains third-party software components governed by the license(s) indicated below:");
        sb.AppendLine();

        if (package.Dependencies != null && package.Dependencies.Count > 0)
            foreach (var dep in package.Dependencies)
            {
                sb.AppendLine($"Component Name: {dep.Name}");
                sb.AppendLine("License Type: \"MIT\"");
                sb.AppendLine($"Version Number: {dep.Version}");
                sb.AppendLine($"[License Link](https://github.com/unity-package-manager/{dep.Name})");
                sb.AppendLine();
            }
        else
            sb.AppendLine("No third-party components are currently listed.");

        return sb.ToString();
    }
}