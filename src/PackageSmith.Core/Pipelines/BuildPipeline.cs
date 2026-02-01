using System;
using System.Collections.Generic;
using System.Linq;
using PackageSmith.Data.Config;
using PackageSmith.Data.State;
using PackageSmith.Data.Types;
using PackageSmith.Core.Interfaces;
using PackageSmith.Core.Extensions;
using PackageSmith.Core.Logic;

namespace PackageSmith.Core.Pipelines;

public sealed class BuildPipeline : IPackageGenerator
{
	public bool TryGenerate(in PackageState package, in AppConfig config, out PackageLayoutState layout)
	{
		layout = default;

		package.TryValidate(out var isValid);
		if (!isValid) return false;

		var hasSubAssemblies = package.HasSubAssemblies();
		var directories = GenerateDirectories(in package, hasSubAssemblies);
		var files = GenerateFiles(in package, in config, hasSubAssemblies);

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
		dirs.Add(new VirtualDirectoryState { Path = new string(basePath) });

		package.TryGetAsmDefRoot(out var asmdefRoot);

		if (hasSubAssemblies)
		{
			AsmDefLogic.GetStandardSubAssemblies(package.PackageName.ToString(), out var subAssemblies);
			foreach (var sub in subAssemblies)
			{
				if (package.SubAssemblies.HasFlag(sub.Type))
				{
					dirs.Add(new VirtualDirectoryState { Path = new string(System.IO.Path.Combine(basePath, sub.Name.ToString())) });
				}
			}
		}
		else if (package.HasModule(PackageModuleType.Runtime))
		{
			dirs.Add(new VirtualDirectoryState { Path = new string(System.IO.Path.Combine(basePath, "Runtime")) });
		}

		if (package.HasModule(PackageModuleType.Editor))
		{
			var editorFolder = hasSubAssemblies ? $"{asmdefRoot}.Editor" : "Editor";
			dirs.Add(new VirtualDirectoryState { Path = new string(System.IO.Path.Combine(basePath, editorFolder)) });
		}

		if (package.HasModule(PackageModuleType.Tests))
		{
			var testsFolder = hasSubAssemblies ? $"{asmdefRoot}.Tests" : "Tests";
			dirs.Add(new VirtualDirectoryState { Path = new string(System.IO.Path.Combine(basePath, testsFolder)) });
		}

		if (package.HasModule(PackageModuleType.Samples))
		{
			dirs.Add(new VirtualDirectoryState { Path = new string(System.IO.Path.Combine(basePath, "Samples~")) });
		}

		dirs.Add(new VirtualDirectoryState { Path = new string(System.IO.Path.Combine(basePath, "Documentation~")) });

		return dirs.ToArray();
	}

	private static VirtualFileState[] GenerateFiles(in PackageState package, in AppConfig config, bool hasSubAssemblies)
	{
		var files = new List<VirtualFileState>();

		package.TryGetBasePath(out var basePath);
		package.TryGetAsmDefRoot(out var asmdefRoot);

		// 1. Manifest, Readme, GitIgnore
		files.Add(new VirtualFileState
		{
			Path = new string(System.IO.Path.Combine(basePath, "package.json")),
			Content = new string(GeneratePackageManifest(in package, in config))
		});

		files.Add(new VirtualFileState
		{
			Path = new string(System.IO.Path.Combine(basePath, "README.md")),
			Content = new string(GenerateReadme(in package))
		});

		files.Add(new VirtualFileState
		{
			Path = new string(System.IO.Path.Combine(basePath, ".gitignore")),
			Content = new string(GitLogic.GenerateGitIgnore())
		});

		// 1.5. License and Changelog
		if (package.License != LicenseType.None)
		{
			files.Add(new VirtualFileState
			{
				Path = new string(System.IO.Path.Combine(basePath, "LICENSE.md")),
				Content = new string(GitLogic.GenerateLicense(package.License, DateTime.Now.Year.ToString(), package.CompanyName))
			});
		}

		files.Add(new VirtualFileState
		{
			Path = new string(System.IO.Path.Combine(basePath, "CHANGELOG.md")),
			Content = new string(GitLogic.GenerateChangelog(package.PackageName, "1.0.0"))
		});

		// 2. Generate AsmDefs
		if (package.HasModule(PackageModuleType.Runtime) && !hasSubAssemblies)
		{
			AsmDefLogic.GetEcsReferences(in package.EcsPreset, out var refs);
			var asmdefContent = AsmDefGenerationLogic.GenerateJson(asmdefRoot, refs, package.EcsPreset.EnableBurst);

			files.Add(new VirtualFileState
			{
				Path = new string(System.IO.Path.Combine(basePath, "Runtime", $"{asmdefRoot}.asmdef")),
				Content = new string(asmdefContent)
			});
		}

		if (package.HasModule(PackageModuleType.Editor))
		{
			var editorAsmdefName = hasSubAssemblies ? $"{asmdefRoot}.Editor" : $"{asmdefRoot}.Editor";
			var runtimeRefs = hasSubAssemblies
				? SubAssemblyLogic.GetRuntimeAssemblies(package.SubAssemblies, asmdefRoot)
				: new[] { asmdefRoot };

			var editorAsmdef = AsmDefGenerationLogic.GenerateEditorJson(editorAsmdefName, runtimeRefs);
			var editorFolder = hasSubAssemblies ? editorAsmdefName : "Editor";
			files.Add(new VirtualFileState
			{
				Path = new string(System.IO.Path.Combine(basePath, editorFolder, $"{editorAsmdefName}.asmdef")),
				Content = new string(editorAsmdef)
			});
		}

		// 3. Generate Code Templates
		package.TryGetNamespace(out var ns);
		var featureName = package.DisplayName.ToString();
		var packageParts = package.PackageName.ToString().Split('.');
		var baseName = packageParts.Length > 0 ? packageParts[^1] : "Feature";

		if (package.SelectedTemplate.HasFlag(TemplateType.MonoBehaviour))
		{
			var code = TemplateLogic.GenerateMonoBehaviour(ns, $"{featureName}Behavior");
			var runtimeFolder = hasSubAssemblies ? $"{asmdefRoot}.Runtime" : "Runtime";
			files.Add(new VirtualFileState
			{
				Path = new string(System.IO.Path.Combine(basePath, runtimeFolder, $"{featureName}Behavior.cs")),
				Content = new string(code)
			});
		}

		if (package.SelectedTemplate.HasFlag(TemplateType.ScriptableObject))
		{
			var code = TemplateLogic.GenerateScriptableObject(ns, $"{featureName}Config");
			var runtimeFolder = hasSubAssemblies ? $"{asmdefRoot}.Runtime" : "Runtime";
			files.Add(new VirtualFileState
			{
				Path = new string(System.IO.Path.Combine(basePath, runtimeFolder, $"{featureName}Config.cs")),
				Content = new string(code)
			});
		}

		if (package.SelectedTemplate.HasFlag(TemplateType.SystemBase))
		{
			var code = TemplateLogic.GenerateSystemBase(ns, $"{featureName}System");
			var systemsFolder = hasSubAssemblies ? $"{asmdefRoot}.Systems" : "Runtime";
			files.Add(new VirtualFileState
			{
				Path = new string(System.IO.Path.Combine(basePath, systemsFolder, $"{featureName}System.cs")),
				Content = new string(code)
			});
		}

		if (package.SelectedTemplate.HasFlag(TemplateType.IComponentData))
		{
			var code = TemplateLogic.GenerateIComponentData(ns, $"{baseName}Component");
			var dataFolder = hasSubAssemblies ? $"{asmdefRoot}.Data" : "Runtime";
			files.Add(new VirtualFileState
			{
				Path = new string(System.IO.Path.Combine(basePath, dataFolder, $"{baseName}Component.cs")),
				Content = new string(code)
			});
		}

		if (package.SelectedTemplate.HasFlag(TemplateType.Authoring))
		{
			var code = TemplateLogic.GenerateAuthoring(ns, baseName, $"{baseName}Component");
			var authoringFolder = hasSubAssemblies ? $"{asmdefRoot}.Authoring" : "Authoring";
			files.Add(new VirtualFileState
			{
				Path = new string(System.IO.Path.Combine(basePath, authoringFolder, $"{baseName}Authoring.cs")),
				Content = new string(code)
			});
		}

		if (package.SelectedTemplate.HasFlag(TemplateType.EcsFull))
		{
			var componentName = $"{baseName}Component";

			// Component Data
			var componentCode = TemplateLogic.GenerateIComponentData(ns, componentName);
			var dataFolder = hasSubAssemblies ? $"{asmdefRoot}.Data" : "Runtime";
			files.Add(new VirtualFileState
			{
				Path = new string(System.IO.Path.Combine(basePath, dataFolder, $"{componentName}.cs")),
				Content = new string(componentCode)
			});

			// Authoring
			if (hasSubAssemblies)
			{
				var authoringNs = $"{ns}.Authoring";
				var authoringCode = TemplateLogic.GenerateAuthoring(authoringNs, baseName, componentName);
				files.Add(new VirtualFileState
				{
					Path = new string(System.IO.Path.Combine(basePath, $"{asmdefRoot}.Authoring", $"{baseName}Authoring.cs")),
					Content = new string(authoringCode)
				});
			}

			// System
			var systemCode = TemplateLogic.GenerateSystemBase(ns, $"{baseName}System");
			var systemsFolder = hasSubAssemblies ? $"{asmdefRoot}.Systems" : "Runtime";
			files.Add(new VirtualFileState
			{
				Path = new string(System.IO.Path.Combine(basePath, systemsFolder, $"{baseName}System.cs")),
				Content = new string(systemCode)
			});
		}

		return files.ToArray();
	}

	private static string GeneratePackageManifest(in PackageState package, in AppConfig config)
	{
		GitLogic.TryGetGitConfig(out var gitName, out var gitEmail);
		var authorName = string.IsNullOrEmpty(gitName) || gitName == "Unknown" ? config.CompanyName.ToString() : gitName;
		var authorEmail = gitEmail == "unknown@local" ? "" : gitEmail;

		var authorObj = string.IsNullOrEmpty(authorEmail)
			? $"{{\"name\": \"{authorName}\"}}"
			: $"{{\"name\": \"{authorName}\", \"email\": \"{authorEmail}\"}}";

		var samples = package.HasModule(PackageModuleType.Samples) ? ",\n\t\"samples\": [{{\"displayName\": \"Demo\", \"path\": \"Samples~/Demo\"}}]" : "";

		return $$"""
		{
			"name": "{{package.PackageName.ToString()}}",
			"version": "1.0.0",
			"displayName": "{{package.DisplayName.ToString()}}",
			"description": "{{package.Description.ToString()}}",
			"unity": "{{config.DefaultUnityVersion.ToString()}}",
			"author": {{authorObj}}{{samples}}
		}
		""";
	}

	private static string GenerateReadme(in PackageState package)
	{
		return $"# {package.DisplayName.ToString()}\n\n{package.Description.ToString()}";
	}
}
