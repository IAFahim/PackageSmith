using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Runtime.CompilerServices;
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GenerateJson(string name, ReferenceState[] references, bool allowUnsafe, VersionDefine[]? versionDefines = null)
	{
		var refNames = references.Select(r => r.Name).ToArray();

		var model = new
		{
			name = name,
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
				? versionDefines.Select(v => new { name = v.Name, expression = v.Expression, define = v.Define }).ToArray()
				: Array.Empty<object>()
		};

		return JsonSerializer.Serialize(model, JsonOptions);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GenerateEditorJson(string name, string[] runtimeReferences, VersionDefine[]? versionDefines = null)
	{
		var model = new
		{
			name = name,
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
				? versionDefines.Select(v => new { name = v.Name, expression = v.Expression, define = v.Define }).ToArray()
				: Array.Empty<object>()
		};

		return JsonSerializer.Serialize(model, JsonOptions);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GenerateTestsJson(string name, string[] runtimeReferences, string[] editorReferences)
	{
		var allRefs = runtimeReferences.Concat(editorReferences).ToArray();

		var model = new
		{
			name = name,
			references = allRefs.Length > 0 ? allRefs : null,
			includePlatforms = (string?)null,
			excludePlatforms = new[] { "Editor" },
			allowUnsafeCode = false,
			autoReferenced = false,
			overrideReferences = false,
			noEngineReferences = false,
			precompiledReferences = new[] { "nunit.framework.dll" },
			defineConstraints = Array.Empty<string>(),
			versionDefines = new[] { new { @if = new[] { "UNITY_EDITOR" } } }
		};

		return JsonSerializer.Serialize(model, JsonOptions);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static VersionDefine[] Unity2022() => new VersionDefine[]
	{
		new() { Name = "com.unity.modules.xr", Expression = "1.0.0", Define = "UNITY_XR_1_0_OR_NEWER" }
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static VersionDefine[] Unity2023() => new VersionDefine[]
	{
		new() { Name = "com.unity.modules.xr", Expression = "1.0.0", Define = "UNITY_XR_1_0_OR_NEWER" },
		new() { Name = "com.unity.input.system", Expression = "1.7.0", Define = "UNITY_INPUT_SYSTEM_1_7_OR_NEWER" }
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static VersionDefine[] HDRP7_1() => new VersionDefine[]
	{
		new() { Name = "com.unity.render-pipelines.high-definition", Expression = "7.1.0", Define = "HDRP_7_1_0_OR_NEWER" }
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static VersionDefine[] URP14() => new VersionDefine[]
	{
		new() { Name = "com.unity.render-pipelines.universal", Expression = "14.0.0", Define = "URP_14_0_OR_NEWER" }
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static VersionDefine[] ParticleSystem1_0() => new VersionDefine[]
	{
		new() { Name = "com.unity.modules.particlesystem", Expression = "1.0.0", Define = "USING_PARTICLE_SYSTEM" }
	};
}
