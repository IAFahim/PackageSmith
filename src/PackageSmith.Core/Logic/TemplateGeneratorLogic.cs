using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace PackageSmith.Core.Logic;

public static class TemplateGeneratorLogic
{
	// Mapping from assembly names to Unity package names for versionDefines
	private static readonly Dictionary<string, string> AssemblyToPackageMap = new()
	{
		// Render Pipelines
		["Unity.RenderPipelines.Core.Runtime"] = "com.unity.render-pipelines.core",
		["Unity.RenderPipelines.HighDefinition.Runtime"] = "com.unity.render-pipelines.high-definition",
		["Unity.RenderPipelines.Universal.Runtime"] = "com.unity.render-pipelines.universal",

		// Input
		["Unity.InputSystem"] = "com.unity.input.system",

		// Physics
		["Unity.Physics"] = "com.unity.physics",
		["Unity.Physics.Custom"] = "com.unity.physics",

		// Entities
		["Unity.Entities"] = "com.unity.entities",
		["Unity.Entities.Graphics"] = "com.unity.entities.graphics",
		["Unity.Entities.Hybrid"] = "com.unity.entities.hybrid",

		// Collections
		["Unity.Collections"] = "com.unity.collections",

		// Other
		["Unity.Burst"] = "com.unity.burst",
		["Unity.Mathematics"] = "com.unity.mathematics",
		["Unity.TextCore"] = "com.unity.textcore",
		["Unity.Splines"] = "com.unity.splines",
		["Unity.Transforms"] = "com.unity.transfers",
		["Unity.Cinemachine"] = "com.unity.cinemachine",
	};

	// Common Unity package version defines
	private static readonly Dictionary<string, (string expression, string @define)> UnityPackageDefines = new()
	{
		// Render Pipelines
		["com.unity.render-pipelines.high-definition"] = ("7.1.0", "HDRP_7_1_0_OR_NEWER"),
		["com.unity.render-pipelines.universal"] = ("14.0.0", "URP_14_0_OR_NEWER"),
		["com.unity.render-pipelines.core"] = ("1.0.0", "RENDER_PIPELINES_CORE_1_0_OR_NEWER"),

		// Input
		["com.unity.input.system"] = ("1.7.0", "UNITY_INPUT_SYSTEM_1_7_OR_NEWER"),

		// Physics
		["com.unity.physics"] = ("1.0.0", "UNITY_PHYSICS_MODULE_1_0_OR_NEWER"),
		["com.unity.physics.modules"] = ("1.0.0", "UNITY_PHYSICS_MODULE_EXISTS"),

		// Entities
		["com.unity.entities"] = ("1.0.0", "UNITY_ENTITIES_EXISTS"),
		["com.unity.entities.graphics"] = ("1.0.0", "UNITY_ENTITIES_GRAPHICS_EXISTS"),
		["com.unity.entities.hybrid"] = ("1.0.0", "UNITY_ENTITIES_HYBRID_EXISTS"),

		// Collections
		["com.unity.collections"] = ("2.1.0", "UNITY_COLLECTIONS_EXISTS"),

		// Other
		["com.unity.burst"] = ("1.8.0", "UNITY_BURST_EXISTS"),
		["com.unity.mathematics"] = ("1.2.0", "UNITY_MATHEMATICS_EXISTS"),
		["com.unity.modules.animation"] = ("1.0.0", "UNITY_ANIMATION_MODULE_EXISTS"),
		["com.unity.modules.audio"] = ("1.0.0", "UNITY_AUDIO_MODULE_EXISTS"),
		["com.unity.modules.particlesystem"] = ("1.0.0", "USING_PARTICLE_SYSTEM"),
		["com.unity.modules.ui"] = ("1.0.0", "UNITY_UI_MODULES_EXISTS"),
		["com.unity.ugui"] = ("2.0.0", "UNITY_UGUI_EXISTS"),
		["com.unity.textcore"] = ("3.0.0", "UNITY_TEXT_CORE_EXISTS"),
		["com.unity.timeline"] = ("1.7.0", "UNITY_TIMELINE_EXISTS"),
		["com.unity.toolchain.win"] = ("2.0.0", "UNITY_TOOLCHAIN_WIN_EXISTS"),
		["com.unity.toolchain.macos"] = ("2.0.0", "UNITY_TOOLCHAIN_MACOS_EXISTS"),
		["com.unity.toolchain.linux"] = ("2.0.0", "UNITY_TOOLCHAIN_LINUX_EXISTS"),
		["com.unity.modules.xr"] = ("1.0.0", "UNITY_XR_1_0_OR_NEWER"),
		["com.unity.cinemachine"] = ("3.1.0", "UNITY_CINEMACHINE_EXISTS"),
		["com.unity.splines"] = ("2.0.0", "UNITY_SPLINES_EXISTS"),
		["com.unity.transfers"] = ("1.0.0", "UNITY_TRANSFERS_EXISTS"),
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGenerateFromTemplate(string templatePath, string outputPath, string packageName, out int processedFiles)
	{
		processedFiles = 0;
		if (!Directory.Exists(templatePath)) return false;

		if (Directory.Exists(outputPath)) return false; // SAFETY: Never auto-delete existing directories
		Directory.CreateDirectory(outputPath);

		var files = Directory.EnumerateFiles(templatePath, "*", SearchOption.AllDirectories);

		foreach (var file in files)
		{
			if (Path.GetFileName(file).StartsWith(".")) continue;

			var relativePath = Path.GetRelativePath(templatePath, file);
			var destFile = Path.Combine(outputPath, DetokenizePath(relativePath, packageName));

			Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

			// Special handling for .asmdef files - inject versionDefines
			if (Path.GetExtension(file).Equals(".asmdef", StringComparison.OrdinalIgnoreCase))
			{
				var content = ProcessAsmDefFile(file, packageName);
				File.WriteAllText(destFile, content);
			}
			else
			{
				var content = File.ReadAllText(file);
				var detokenizedContent = DetokenizeString(content, packageName);
				File.WriteAllText(destFile, detokenizedContent);
			}

			processedFiles++;
		}

		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string ProcessAsmDefFile(string asmdefPath, string packageName)
	{
		var content = File.ReadAllText(asmdefPath);
		var detokenized = DetokenizeString(content, packageName);

		// Parse and inject versionDefines
		var doc = JsonDocument.Parse(detokenized);
		var root = doc.RootElement;

		// Get references
		var references = new List<string>();
		if (root.TryGetProperty("references", out var refsProp) && refsProp.ValueKind == JsonValueKind.Array)
		{
			foreach (var refElem in refsProp.EnumerateArray())
			{
				if (refElem.ValueKind == JsonValueKind.String)
				{
					references.Add(refElem.GetString() ?? string.Empty);
				}
			}
		}

		// Build versionDefines for Unity packages
		var versionDefines = new Dictionary<string, (string expression, string @define)>();
		foreach (var refName in references)
		{
			// Try direct package name match first
			if (UnityPackageDefines.TryGetValue(refName, out var versionDef))
			{
				versionDefines[refName] = versionDef;
			}
			// Try assembly name mapping
			else if (AssemblyToPackageMap.TryGetValue(refName, out var mappedPackage) && UnityPackageDefines.TryGetValue(mappedPackage, out versionDef))
			{
				versionDefines[mappedPackage] = versionDef;
			}
		}

		// Add existing versionDefines if any, merging duplicates by name
		if (root.TryGetProperty("versionDefines", out var existingProp) && existingProp.ValueKind == JsonValueKind.Array)
		{
			foreach (var vd in existingProp.EnumerateArray())
			{
				if (vd.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
				{
					var name = nameProp.GetString();
					if (!string.IsNullOrEmpty(name) && !versionDefines.ContainsKey(name))
					{
						var expression = vd.TryGetProperty("expression", out var expProp) ? expProp.GetString() ?? string.Empty : string.Empty;
						var define = vd.TryGetProperty("define", out var defProp) ? defProp.GetString() ?? string.Empty : string.Empty;
						versionDefines[name] = (expression, define);
					}
				}
			}
		}

		// Build new JSON using JsonObject for mutable manipulation
		using var stream = new MemoryStream();
		using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

		writer.WriteStartObject();

		// Copy all existing properties except versionDefines
		foreach (var prop in root.EnumerateObject())
		{
			if (prop.NameEquals("versionDefines")) continue;
			if (prop.NameEquals("references"))
			{
				// Write detokenized references
				var asmName = GetAssemblyNameFromPackage(packageName);
				writer.WritePropertyName(prop.Name);
				writer.WriteStartArray();
				foreach (var refName in references)
				{
					var detokenizedRef = refName.Replace("{{ASM_NAME}}", asmName);
					writer.WriteStringValue(detokenizedRef);
				}
				writer.WriteEndArray();
			}
			else
			{
				prop.WriteTo(writer);
			}
		}

		// Write versionDefines
		if (versionDefines.Count > 0)
		{
			writer.WritePropertyName("versionDefines");
			writer.WriteStartArray();
			foreach (var (name, (expression, define)) in versionDefines)
			{
				writer.WriteStartObject();
				writer.WriteString("name", name);
				writer.WriteString("expression", expression);
				writer.WriteString("define", define);
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
		}

		writer.WriteEndObject();
		writer.Flush();

		return System.Text.Encoding.UTF8.GetString(stream.ToArray());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string DetokenizeString(string input, string packageName)
	{
		if (string.IsNullOrEmpty(input)) return input;

		var asmName = GetAssemblyNameFromPackage(packageName);
		var asmShortName = GetShortName(asmName);

		var result = input
			.Replace("{{PACKAGE_NAME}}", packageName)
			.Replace("{{ASM_NAME}}", asmName)
			.Replace("{{ASM_SHORT_NAME}}", asmShortName)
			.Replace("{{PACKAGE_PASCAL_NAME}}", asmName);

		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string DetokenizePath(string path, string packageName)
	{
		var asmName = GetAssemblyNameFromPackage(packageName);

		return path
			.Replace("{{PACKAGE_NAME}}", asmName)
			.Replace("{{ASM_NAME}}", asmName)
			.Replace("{{ASM_SHORT_NAME}}", asmName)
			.Replace("{{PACKAGE_PASCAL_NAME}}", asmName);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string GetAssemblyNameFromPackage(string packageName)
	{
		var parts = packageName.Split('.');
		var result = new StringBuilder();

		for (int i = 1; i < parts.Length; i++)
		{
			var part = parts[i];
			if (string.IsNullOrEmpty(part)) continue;

			// Sanitize: Remove hyphens and convert to PascalCase
			// e.g., "my-company" -> "MyCompany"
			var sanitized = SanitizeToPascalCase(part);
			if (result.Length > 0)
				result.Append('.');
			result.Append(sanitized);
		}

		return result.Length > 0 ? result.ToString() : "MyNamespace";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string SanitizeToPascalCase(string input)
	{
		if (string.IsNullOrEmpty(input)) return input;

		// Split by hyphens, capitalize each part, then join
		var parts = input.Split('-', '_');
		var result = new StringBuilder();

		foreach (var part in parts)
		{
			if (string.IsNullOrEmpty(part)) continue;

			// Capitalize first letter, make rest lowercase
			var sanitized = char.ToUpper(part[0], CultureInfo.InvariantCulture) +
				(part.Length > 1 ? part.Substring(1).ToLower(CultureInfo.InvariantCulture) : "");
			result.Append(sanitized);
		}

		return result.Length > 0 ? result.ToString() : input;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string GetShortName(string asmName)
	{
		var parts = asmName.Split('.');
		return parts.Length > 0 ? parts[^1] : asmName;
	}
}
