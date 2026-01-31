using System;
using System.Collections.Generic;
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
		dirs.Add(new VirtualDirectoryState { Path = new FixedString64(basePath) });

		package.TryGetAsmDefRoot(out var asmdefRoot);

		if (hasSubAssemblies)
		{
			AsmDefLogic.GetStandardSubAssemblies(package.PackageName.ToString(), out var subAssemblies);
			foreach (var sub in subAssemblies)
			{
				if (package.SubAssemblies.HasFlag(sub.Type))
				{
					dirs.Add(new VirtualDirectoryState { Path = new FixedString64(System.IO.Path.Combine(basePath, sub.Name.ToString())) });
				}
			}
		}
		else if (package.HasModule(PackageModuleType.Runtime))
		{
			dirs.Add(new VirtualDirectoryState { Path = new FixedString64(System.IO.Path.Combine(basePath, "Runtime")) });
		}

		if (package.HasModule(PackageModuleType.Editor))
		{
			var editorFolder = hasSubAssemblies ? $"{asmdefRoot}.Editor" : "Editor";
			dirs.Add(new VirtualDirectoryState { Path = new FixedString64(System.IO.Path.Combine(basePath, editorFolder)) });
		}

		if (package.HasModule(PackageModuleType.Tests))
		{
			var testsFolder = hasSubAssemblies ? $"{asmdefRoot}.Tests" : "Tests";
			dirs.Add(new VirtualDirectoryState { Path = new FixedString64(System.IO.Path.Combine(basePath, testsFolder)) });
		}

		if (package.HasModule(PackageModuleType.Samples))
		{
			dirs.Add(new VirtualDirectoryState { Path = new FixedString64(System.IO.Path.Combine(basePath, "Samples~")) });
		}

		dirs.Add(new VirtualDirectoryState { Path = new FixedString64(System.IO.Path.Combine(basePath, "Documentation~")) });

		return dirs.ToArray();
	}

	private static VirtualFileState[] GenerateFiles(in PackageState package, in AppConfig config, bool hasSubAssemblies)
	{
		var files = new List<VirtualFileState>();

		package.TryGetBasePath(out var basePath);
		package.TryGetAsmDefRoot(out var asmdefRoot);

		var manifest = GeneratePackageManifest(in package, in config);
		files.Add(new VirtualFileState
		{
			Path = new FixedString64(System.IO.Path.Combine(basePath, "package.json")),
			Content = new FixedString64(manifest)
		});

		var readme = GenerateReadme(in package);
		files.Add(new VirtualFileState
		{
			Path = new FixedString64(System.IO.Path.Combine(basePath, "README.md")),
			Content = new FixedString64(readme)
		});

		return files.ToArray();
	}

	private static string GeneratePackageManifest(in PackageState package, in AppConfig config)
	{
		return $$"""
		{
			"name": "{{package.PackageName.ToString()}}",
			"version": "1.0.0",
			"displayName": "{{package.DisplayName.ToString()}}",
			"description": "{{package.Description.ToString()}}",
			"unity": "{{config.DefaultUnityVersion.ToString()}}",
			"author": "{{config.CompanyName.ToString()}}"
		}
		""";
	}

	private static string GenerateReadme(in PackageState package)
	{
		return $"# {package.DisplayName.ToString()}\n\n{package.Description.ToString()}";
	}
}
