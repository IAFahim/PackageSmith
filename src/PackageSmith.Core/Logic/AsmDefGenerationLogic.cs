using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using PackageSmith.Data.State;

namespace PackageSmith.Core.Logic;

public sealed class VersionDefine
{
    public string Name { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public string Define { get; set; } = string.Empty;
}

public static class AsmDefGenerationLogic
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly Dictionary<string, string> AssemblyToPackageMap = new()
    {
        ["Unity.InputSystem"] = "com.unity.input.system",
        ["Unity.Physics"] = "com.unity.physics",
        ["Unity.CharacterController"] = "com.unity.charactercontroller",
        ["Unity.Entities"] = "com.unity.entities",
        ["Unity.Entities.Graphics"] = "com.unity.entities.graphics",
        ["Unity.Entities.Hybrid"] = "com.unity.entities.hybrid",
        ["Unity.Collections"] = "com.unity.collections",
        ["Unity.Mathematics"] = "com.unity.mathematics",
        ["Unity.Burst"] = "com.unity.burst",
        ["Unity.Jobs"] = "com.unity.jobs",
        ["Unity.NetCode"] = "com.unity.netcode.gameobjects",
        ["Unity.Networking.Transport"] = "com.unity.transport",
        ["Unity.RenderPipelines.Core"] = "com.unity.render-pipelines.core",
        ["Unity.RenderPipelines.Universal.Runtime"] = "com.unity.render-pipelines.universal",
        ["Unity.RenderPipelines.HighDefinition.Runtime"] = "com.unity.render-pipelines.high-definition",
        ["Unity.Addressables"] = "com.unity.addressables",
        ["Unity.TextMeshPro"] = "com.unity.textmeshpro"
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DerivePackageId(string assemblyName)
    {
        if (AssemblyToPackageMap.TryGetValue(assemblyName, out var pkg)) return pkg;

        var lower = assemblyName.ToLowerInvariant();
        if (assemblyName.StartsWith("Unity.", StringComparison.OrdinalIgnoreCase)) return "com." + lower;

        if (lower.Contains('.'))
        {
            if (!lower.StartsWith("com.") && !lower.StartsWith("net.") && !lower.StartsWith("org."))
                return "com." + lower;
            return lower;
        }

        return lower;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DeriveDefineSymbol(string assemblyName)
    {
        if (assemblyName.Contains(":")) return "HAS_UNKNOWN_REF";
        var sb = new StringBuilder("HAS_");
        foreach (var c in assemblyName)
            if (c == '.') sb.Append('_');
            else if (char.IsLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
        return sb.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VersionDefine[] GenerateVersionDefines(IEnumerable<string> references, string? selfPackageId = null,
        VersionDefine[]? existingDefines = null)
    {
        var defines = new List<VersionDefine>();
        var knownIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (existingDefines != null)
            foreach (var d in existingDefines)
            {
                defines.Add(d);
                knownIds.Add(d.Name);
            }

        if (!string.IsNullOrEmpty(selfPackageId) && !knownIds.Contains(selfPackageId))
        {
            defines.Add(new VersionDefine
            {
                Name = selfPackageId,
                Expression = "0.0.1",
                Define = DeriveDefineSymbol(selfPackageId.Contains('.')
                    ? selfPackageId.Split('.').Last()
                    : selfPackageId)
            });
            knownIds.Add(selfPackageId);
        }

        foreach (var refName in references)
        {
            var packageId = DerivePackageId(refName);
            if (knownIds.Contains(packageId)) continue;

            defines.Add(new VersionDefine
            {
                Name = packageId,
                Expression = "0.0.1",
                Define = DeriveDefineSymbol(refName)
            });
            knownIds.Add(packageId);
        }

        return defines.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GenerateJson(string name, ReferenceState[] references, bool allowUnsafe,
        VersionDefine[]? versionDefines = null)
    {
        var refNames = references.Select(r => r.Name).ToArray();

        var model = new
        {
            name,
            references = refNames.Length > 0 ? refNames : null,
            includePlatforms = Array.Empty<string>(),
            excludePlatforms = Array.Empty<string>(),
            allowUnsafeCode = allowUnsafe,
            autoReferenced = true,
            overrideReferences = false,
            noEngineReferences = false,
            precompiledReferences = Array.Empty<string>(),
            defineConstraints = Array.Empty<string>(),
            versionDefines = versionDefines != null && versionDefines.Length > 0
                ? versionDefines.Select(v => new { name = v.Name, expression = v.Expression, define = v.Define })
                    .ToArray()
                : Array.Empty<object>()
        };

        return JsonSerializer.Serialize(model, JsonOptions);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GenerateEditorJson(string name, string[] runtimeReferences,
        VersionDefine[]? versionDefines = null)
    {
        var model = new
        {
            name,
            references = runtimeReferences.Length > 0 ? runtimeReferences : null,
            includePlatforms = new[] { "Editor" },
            excludePlatforms = Array.Empty<string>(),
            allowUnsafeCode = false,
            autoReferenced = true,
            overrideReferences = false,
            noEngineReferences = false,
            precompiledReferences = Array.Empty<string>(),
            defineConstraints = Array.Empty<string>(),
            versionDefines = versionDefines != null && versionDefines.Length > 0
                ? versionDefines.Select(v => new { name = v.Name, expression = v.Expression, define = v.Define })
                    .ToArray()
                : Array.Empty<object>()
        };

        return JsonSerializer.Serialize(model, JsonOptions);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GenerateTestsJson(string name, string[] runtimeReferences, string[] editorReferences,
        string[]? includePlatforms = null, VersionDefine[]? versionDefines = null)
    {
        var allRefs = runtimeReferences.Concat(editorReferences).ToArray();

        var model = new
        {
            name,
            references = allRefs.Length > 0 ? allRefs : null,
            includePlatforms = includePlatforms ?? new[] { "Editor" },
            excludePlatforms = Array.Empty<string>(),
            allowUnsafeCode = false,
            autoReferenced = false,
            overrideReferences = true,
            noEngineReferences = false,
            precompiledReferences = new[] { "nunit.framework.dll" },
            defineConstraints = new[] { "UNITY_INCLUDE_TESTS" },
            versionDefines = versionDefines != null && versionDefines.Length > 0
                ? versionDefines.Select(v => new { name = v.Name, expression = v.Expression, define = v.Define })
                    .ToArray()
                : Array.Empty<object>()
        };

        return JsonSerializer.Serialize(model, JsonOptions);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VersionDefine[] Unity2022()
    {
        return new VersionDefine[]
        {
            new() { Name = "com.unity.modules.xr", Expression = "1.0.0", Define = "UNITY_XR_1_0_OR_NEWER" }
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VersionDefine[] Unity2023()
    {
        return new VersionDefine[]
        {
            new() { Name = "com.unity.modules.xr", Expression = "1.0.0", Define = "UNITY_XR_1_0_OR_NEWER" },
            new() { Name = "com.unity.input.system", Expression = "1.7.0", Define = "UNITY_INPUT_SYSTEM_1_7_OR_NEWER" }
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VersionDefine[] HDRP7_1()
    {
        return new VersionDefine[]
        {
            new()
            {
                Name = "com.unity.render-pipelines.high-definition", Expression = "7.1.0",
                Define = "HDRP_7_1_0_OR_NEWER"
            }
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VersionDefine[] URP14()
    {
        return new VersionDefine[]
        {
            new() { Name = "com.unity.render-pipelines.universal", Expression = "14.0.0", Define = "URP_14_0_OR_NEWER" }
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VersionDefine[] ParticleSystem1_0()
    {
        return new VersionDefine[]
        {
            new() { Name = "com.unity.modules.particlesystem", Expression = "1.0.0", Define = "USING_PARTICLE_SYSTEM" }
        };
    }
}