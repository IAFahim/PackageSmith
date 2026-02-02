using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using PackageSmith.Data.State;
using PackageSmith.Data.Types;

namespace PackageSmith.Core.Logic;

public static class PackageLogic
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ValidatePackageName(in string packageName, out bool isValid)
    {
        isValid = !string.IsNullOrWhiteSpace(packageName) && packageName.StartsWith("com.");
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
        combinedPath = Path.Combine(basePath, relativePath);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetAsmDefRoot(in string packageName, out string asmdefRoot)
    {
        var parts = packageName.Split('.');
        var startIndex = parts.Length > 0 && parts[0] is "com" or "net" or "org" or "io" ? 1 : 0;
        var relevantParts = parts.Skip(startIndex);

        var sb = new StringBuilder();
        foreach (var part in relevantParts)
        {
            if (sb.Length > 0) sb.Append('.');
            sb.Append(SanitizeToPascalCase(part));
        }

        asmdefRoot = sb.ToString();
        if (string.IsNullOrEmpty(asmdefRoot)) asmdefRoot = SanitizeToPascalCase(packageName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GenerateNamespace(in string packageName, out string ns)
    {
        GetAsmDefRoot(packageName, out ns);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string SanitizeToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var parts = input.Split('-', '_');
        var result = new StringBuilder();
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;
            var sanitized = char.ToUpper(part[0], CultureInfo.InvariantCulture) +
                            (part.Length > 1 ? part.Substring(1).ToLower(CultureInfo.InvariantCulture) : "");
            result.Append(sanitized);
        }

        return result.Length > 0 ? result.ToString() : input;
    }
}