using System;
using System.Linq;
using System.Runtime.CompilerServices;
using PackageSmith.Data.State;
using PackageSmith.Data.Types;

namespace PackageSmith.Core.Logic;

public static class PackageLogic
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidatePackageName(in string packageName, out bool isValid)
	{
		var str = packageName.ToString();
		isValid = !string.IsNullOrWhiteSpace(str) && str.StartsWith("com.");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void IsPackageValid(in PackageState package, out bool isValid)
	{
		ValidatePackageName(package.PackageName, out isValid);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void HasSubAssemblies(in PackageState package, out bool hasSubAssemblies)
	{
		hasSubAssemblies = package.EnableSubAssemblies && package.SubAssemblies != SubAssemblyType.None;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void IsEcsEnabled(in EcsPresetState preset, out bool isEnabled)
	{
		isEnabled = preset.EnableEntities || preset.EnableBurst || preset.EnableCollections;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void HasDependencies(in PackageState package, out bool hasDependencies)
	{
		hasDependencies = package.DependencyCount > 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void HasTemplate(in PackageState package, out bool hasTemplate)
	{
		hasTemplate = package.SelectedTemplate != TemplateType.None;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void HasModule(in PackageState package, PackageModuleType module, out bool hasModule)
	{
		hasModule = (package.SelectedModules & module) == module;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CombinePath(in string basePath, in string relativePath, out string combinedPath)
	{
		combinedPath = System.IO.Path.Combine(basePath.ToString(), relativePath.ToString());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GetAsmDefRoot(in string packageName, out string asmdefRoot)
	{
		var parts = packageName.ToString().Split('.');
		asmdefRoot = parts.Length > 0 ? parts[^1] : packageName.ToString();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GenerateNamespace(in string packageName, out string ns)
	{
		var parts = packageName.ToString().Split('.');
		ns = string.Join(".", parts.Skip(2).Take(parts.Length - 2));
	}
}
