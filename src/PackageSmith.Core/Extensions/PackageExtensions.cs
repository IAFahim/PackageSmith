using PackageSmith.Data.State;
using PackageSmith.Data.Types;
using PackageSmith.Core.Logic;

namespace PackageSmith.Core.Extensions;

public static class PackageExtensions
{
	public static bool TryValidate(in this PackageState package, out bool isValid)
	{
		PackageLogic.IsPackageValid(in package, out isValid);
		return isValid;
	}

	public static bool TryGetAsmDefRoot(in this PackageState package, out string root)
	{
		PackageLogic.GetAsmDefRoot(package.PackageName, out root);
		return !string.IsNullOrEmpty(root);
	}

	public static bool TryGetNamespace(in this PackageState package, out string ns)
	{
		PackageLogic.GenerateNamespace(package.PackageName, out ns);
		return !string.IsNullOrEmpty(ns);
	}

	public static bool TryGetBasePath(in this PackageState package, out string basePath)
	{
		PackageLogic.CombinePath(package.OutputPath, package.PackageName, out basePath);
		return !string.IsNullOrEmpty(basePath);
	}

	public static bool HasSubAssemblies(in this PackageState package)
	{
		PackageLogic.HasSubAssemblies(in package, out var hasSub);
		return hasSub;
	}

	public static bool IsEcsEnabled(in this PackageState package)
	{
		PackageLogic.IsEcsEnabled(in package.EcsPreset, out var isEnabled);
		return isEnabled;
	}

	public static bool HasDependencies(in this PackageState package)
	{
		PackageLogic.HasDependencies(in package, out var hasDeps);
		return hasDeps;
	}

	public static bool HasTemplate(in this PackageState package)
	{
		PackageLogic.HasTemplate(in package, out var hasTmpl);
		return hasTmpl;
	}

	public static bool HasModule(in this PackageState package, PackageModuleType module)
	{
		PackageLogic.HasModule(in package, module, out var hasMod);
		return hasMod;
	}
}
