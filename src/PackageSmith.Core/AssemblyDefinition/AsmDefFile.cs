namespace PackageSmith.Core.AssemblyDefinition;

[Serializable]
public readonly struct AsmDefFile
{
    public readonly string Name;
    public readonly AsmDefReference[] References;
    public readonly AsmDefPlatform[] IncludePlatforms;
    public readonly AsmDefPlatform[] ExcludePlatforms;
    public readonly string[] DefineConstraints;
    public readonly bool AllowUnsafeCode;
    public readonly bool OverrideReferences;
    public readonly bool AutoReferenced;
    public readonly bool NoEngineReferences;
    public readonly string[] VersionDefines;

    public AsmDefFile(
        string name,
        AsmDefReference[]? references = null,
        AsmDefPlatform[]? includePlatforms = null,
        AsmDefPlatform[]? excludePlatforms = null,
        string[]? defineConstraints = null,
        bool allowUnsafeCode = false,
        bool overrideReferences = false,
        bool autoReferenced = false,
        bool noEngineReferences = false,
        string[]? versionDefines = null)
    {
        Name = name;
        References = references ?? Array.Empty<AsmDefReference>();
        IncludePlatforms = includePlatforms ?? Array.Empty<AsmDefPlatform>();
        ExcludePlatforms = excludePlatforms ?? Array.Empty<AsmDefPlatform>();
        DefineConstraints = defineConstraints ?? Array.Empty<string>();
        AllowUnsafeCode = allowUnsafeCode;
        OverrideReferences = overrideReferences;
        AutoReferenced = autoReferenced;
        NoEngineReferences = noEngineReferences;
        VersionDefines = versionDefines ?? Array.Empty<string>();
    }

    public readonly string ToJson()
    {
        var references = References.Length > 0
            ? $"\n    \"references\": [{string.Join(",\n      ", References.Select(r => $"\"{r.Name}\""))}],"
            : "";

        var includePlatforms = IncludePlatforms.Length > 0
            ? $"\n    \"includePlatforms\": [{string.Join(", ", IncludePlatforms.Select(p => $"\"{ToPlatformString(p)}\""))}],"
            : "";

        var excludePlatforms = ExcludePlatforms.Length > 0
            ? $"\n    \"excludePlatforms\": [{string.Join(", ", ExcludePlatforms.Select(p => $"\"{ToPlatformString(p)}\""))}],"
            : "";

        var defineConstraints = DefineConstraints.Length > 0
            ? $"\n    \"defineConstraints\": [{string.Join(", ", DefineConstraints.Select(d => $"\"{d}\""))}],"
            : "";

        var versionDefines = VersionDefines.Length > 0
            ? $"\n    \"versionDefines\": [{string.Join(", ", VersionDefines.Select(v => $"\"{v}\""))}],"
            : "";

        return $$"""
        {
            "name": "{{Name}}",{{references}}{{includePlatforms}}{{excludePlatforms}}
            "allowUnsafeCode": {{AllowUnsafeCode.ToString().ToLower()}},
            "overrideReferences": {{OverrideReferences.ToString().ToLower()}},
            "precompiledReferences": [],{{defineConstraints}}
            "autoReferenced": {{AutoReferenced.ToString().ToLower()}},{{versionDefines}}
            "noEngineReferences": {{NoEngineReferences.ToString().ToLower()}}
        }
        """;
    }

    private static string ToPlatformString(AsmDefPlatform platform)
    {
        return platform switch
        {
            AsmDefPlatform.Editor => "Editor",
            AsmDefPlatform.Windows => "Windows",
            AsmDefPlatform.Linux => "Linux",
            AsmDefPlatform.MacOS => "OSX",
            AsmDefPlatform.Android => "Android",
            AsmDefPlatform.iOS => "iOS",
            AsmDefPlatform.WebGL => "WebGL",
            _ => "AnyPlatform"
        };
    }
}
