using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PackageSmith.Data.State;

namespace PackageSmith.Core.Logic;

public static class TemplateGeneratorLogic
{
	// Mapping from assembly names to known Unity package names
	private static readonly Dictionary<string, string> AssemblyToPackageMap = new()
	{
		// Input
		["Unity.InputSystem"] = "com.unity.input.system",

		// Physics
		["Unity.Physics"] = "com.unity.physics",
		["Unity.Physics.Custom"] = "com.unity.physics",
		["Unity.CharacterController"] = "com.unity.charactercontroller",

		// Entities
		["Unity.Entities"] = "com.unity.entities",
		["Unity.Entities.Graphics"] = "com.unity.entities.graphics",
		["Unity.Entities.Hybrid"] = "com.unity.entities.hybrid",
		["Unity.Scenes"] = "com.unity.entities", // Part of core

		// Collections & Math
		["Unity.Collections"] = "com.unity.collections",
		["Unity.Mathematics"] = "com.unity.mathematics",
		["Unity.Burst"] = "com.unity.burst",
		["Unity.Jobs"] = "com.unity.jobs",

		// Netcode
		["Unity.NetCode"] = "com.unity.netcode.gameobjects",
		["Unity.NetCode.Physics"] = "com.unity.netcode.gameobjects",
		["Unity.Networking.Transport"] = "com.unity.transport",

		// Graphics
		["Unity.RenderPipelines.Core"] = "com.unity.render-pipelines.core",
		["Unity.RenderPipelines.Universal.Runtime"] = "com.unity.render-pipelines.universal",
		["Unity.RenderPipelines.HighDefinition.Runtime"] = "com.unity.render-pipelines.high-definition",

		// Utils
		["Unity.Addressables"] = "com.unity.addressables",
		["Unity.ResourceManager"] = "com.unity.addressables",
		["Unity.TextMeshPro"] = "com.unity.textmeshpro",
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGenerateFromTemplate(string templatePath, string outputPath, string packageName, out int processedFiles)
	{
		processedFiles = 0;
		if (!Directory.Exists(templatePath)) return false;

		if (Directory.Exists(outputPath)) // SAFETY: Check if directory exists and is not empty
		{
			if (Directory.EnumerateFileSystemEntries(outputPath).Any())
				return false;
		}
		Directory.CreateDirectory(outputPath);

		var files = Directory.EnumerateFiles(templatePath, "*", SearchOption.AllDirectories);

		foreach (var file in files)
		{
			if (Path.GetFileName(file).StartsWith(".")) continue; // Skip hidden

			var relativePath = Path.GetRelativePath(templatePath, file);
			var destFile = Path.Combine(outputPath, DetokenizePath(relativePath, packageName));

			Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

			if (Path.GetExtension(file).Equals(".asmdef", StringComparison.OrdinalIgnoreCase)) // Special handling for .asmdef files - inject versionDefines
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

	// Template generation with user-provided metadata
	public static bool TryGenerateFromTemplate(
		string templatePath,
		string outputPath,
		string packageName,
		string displayName,
		string description,
		string authorName,
		string unityVersion,
		out int processedFiles)
	{
		processedFiles = 0;
		if (!Directory.Exists(templatePath)) return false;

		if (Directory.Exists(outputPath))
		{
			if (Directory.EnumerateFileSystemEntries(outputPath).Any())
				return false;
		}
		Directory.CreateDirectory(outputPath);

		var files = Directory.EnumerateFiles(templatePath, "*", SearchOption.AllDirectories);

		foreach (var file in files)
		{
			if (Path.GetFileName(file).StartsWith(".")) continue;

			var relativePath = Path.GetRelativePath(templatePath, file);
			var destFile = Path.Combine(outputPath, DetokenizePath(relativePath, packageName));

			Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

			if (Path.GetExtension(file).Equals(".asmdef", StringComparison.OrdinalIgnoreCase))
			{
				var content = ProcessAsmDefFile(file, packageName, displayName, description, authorName, unityVersion);
				File.WriteAllText(destFile, content);
			}
			else
			{
				var content = File.ReadAllText(file);
				var detokenizedContent = DetokenizeString(content, packageName, displayName, description, authorName, unityVersion);
				File.WriteAllText(destFile, detokenizedContent);
			}

			processedFiles++;
		}

		return true;
	}

	// PURE: Returns virtual files instead of writing to disk (for Transactional I/O)
	public static bool TryGenerateState(
		string templatePath,
		string packageName,
		string displayName,
		string description,
		string authorName,
		string unityVersion,
		out VirtualFileState[] virtualFiles)
	{
		virtualFiles = Array.Empty<VirtualFileState>();
		if (!Directory.Exists(templatePath)) return false;

		var filesList = new List<VirtualFileState>();
		var sourceFiles = Directory.EnumerateFiles(templatePath, "*", SearchOption.AllDirectories);

		foreach (var file in sourceFiles)
		{
			if (Path.GetFileName(file).StartsWith(".")) continue;

			var relativePath = Path.GetRelativePath(templatePath, file);
			var destPath = DetokenizePath(relativePath, packageName);
			string content;

			if (Path.GetExtension(file).Equals(".asmdef", StringComparison.OrdinalIgnoreCase))
			{
				content = ProcessAsmDefFile(file, packageName, displayName, description, authorName, unityVersion);
			}
			else
			{
				var raw = File.ReadAllText(file);
				content = DetokenizeString(raw, packageName, displayName, description, authorName, unityVersion);
			}

			filesList.Add(new VirtualFileState
			{
				Path = destPath,
				Content = content,
				ContentLength = content.Length
			});
		}

		virtualFiles = filesList.ToArray();
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string ProcessAsmDefFile(string asmdefPath, string packageName)
	{
		var content = File.ReadAllText(asmdefPath);
		var detokenized = DetokenizeString(content, packageName);

		using var doc = JsonDocument.Parse(detokenized);
		var root = doc.RootElement;

		var references = new List<string>(); // 1. Extract References
		if (root.TryGetProperty("references", out var refsProp) && refsProp.ValueKind == JsonValueKind.Array)
		{
			foreach (var refElem in refsProp.EnumerateArray())
			{
				if (refElem.ValueKind == JsonValueKind.String)
					references.Add(refElem.GetString() ?? string.Empty);
			}
		}

		var versionDefines = new Dictionary<string, (string expression, string @define)>(); // 2. Build map of Existing Version Defines to avoid duplicates
		if (root.TryGetProperty("versionDefines", out var existingProp) && existingProp.ValueKind == JsonValueKind.Array)
		{
			foreach (var vd in existingProp.EnumerateArray())
			{
				if (vd.TryGetProperty("name", out var nameProp))
				{
					var name = nameProp.GetString();
					if (!string.IsNullOrEmpty(name))
					{
						var exp = vd.TryGetProperty("expression", out var e) ? e.GetString() : "";
						var def = vd.TryGetProperty("define", out var d) ? d.GetString() : "";
						versionDefines[name] = (exp ?? "", def ?? "");
					}
				}
			}
		}

		foreach (var refName in references) // 3. Auto-Generate Version Defines for ALL References
		{
			var packageId = DerivePackageId(refName); // Skip if this reference already has a rule in existing versionDefines. Note: versionDefines usually target Package IDs, but refs are Assembly Names. We check if we have a mapped package ID that is already defined.

			if (versionDefines.ContainsKey(packageId)) continue;

			var defineSymbol = DeriveDefineSymbol(refName); // Create Define Symbol

			versionDefines[packageId] = ("0.0.1", defineSymbol); // Add to list (Expression 0.0.1 means "if present")
		}

		using var stream = new MemoryStream(); // 4. Write output
		using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

		writer.WriteStartObject();

		foreach (var prop in root.EnumerateObject())
		{
			if (prop.NameEquals("versionDefines")) continue;
			if (prop.NameEquals("references"))
			{
				var asmName = GetAssemblyNameFromPackage(packageName);
				writer.WritePropertyName(prop.Name);
				writer.WriteStartArray();
				foreach (var refName in references)
				{
					var finalRef = refName.Replace("{{ASM_NAME}}", asmName);
					writer.WriteStringValue(finalRef);
				}
				writer.WriteEndArray();
			}
			else
			{
				prop.WriteTo(writer);
			}
		}

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

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	// ProcessAsmDefFile with metadata for detokenization
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string ProcessAsmDefFile(string asmdefPath, string packageName, string displayName, string description, string authorName, string unityVersion)
	{
		var content = File.ReadAllText(asmdefPath);
		var detokenized = DetokenizeString(content, packageName, displayName, description, authorName, unityVersion);

		using var doc = JsonDocument.Parse(detokenized);
		var root = doc.RootElement;

		var references = new List<string>();
		if (root.TryGetProperty("references", out var refsProp) && refsProp.ValueKind == JsonValueKind.Array)
		{
			foreach (var refElem in refsProp.EnumerateArray())
			{
				if (refElem.ValueKind == JsonValueKind.String)
					references.Add(refElem.GetString() ?? string.Empty);
			}
		}

		var versionDefines = new Dictionary<string, (string expression, string @define)>();
		if (root.TryGetProperty("versionDefines", out var existingProp) && existingProp.ValueKind == JsonValueKind.Array)
		{
			foreach (var vd in existingProp.EnumerateArray())
			{
				if (vd.TryGetProperty("name", out var nameProp))
				{
					var name = nameProp.GetString();
					if (!string.IsNullOrEmpty(name))
					{
						var exp = vd.TryGetProperty("expression", out var e) ? e.GetString() : "";
						var def = vd.TryGetProperty("define", out var d) ? d.GetString() : "";
						versionDefines[name] = (exp ?? "", def ?? "");
					}
				}
			}
		}

		foreach (var refName in references)
		{
			var packageId = DerivePackageId(refName);

			if (versionDefines.ContainsKey(packageId)) continue;

			var defineSymbol = DeriveDefineSymbol(refName);

			versionDefines[packageId] = ("0.0.1", defineSymbol);
		}

		using var stream = new MemoryStream();
		using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

		writer.WriteStartObject();

		foreach (var prop in root.EnumerateObject())
		{
			if (prop.NameEquals("versionDefines")) continue;
			if (prop.NameEquals("references"))
			{
				var asmName = GetAssemblyNameFromPackage(packageName);
				writer.WritePropertyName(prop.Name);
				writer.WriteStartArray();
				foreach (var refName in references)
				{
					var finalRef = refName.Replace("{{ASM_NAME}}", asmName);
					writer.WriteStringValue(finalRef);
				}
				writer.WriteEndArray();
			}
			else
			{
				prop.WriteTo(writer);
			}
		}

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

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string DerivePackageId(string assemblyName)
	{
		if (AssemblyToPackageMap.TryGetValue(assemblyName, out var pkg)) return pkg; // 1. Check direct map

		var lower = assemblyName.ToLowerInvariant(); // 2. Heuristic: "Unity.Something" -> "com.unity.something", 3. Heuristic: "Company.Product" -> "com.company.product"

		if (assemblyName.StartsWith("Unity.", StringComparison.OrdinalIgnoreCase))
		{
			return "com." + lower;
		}

		// Generic fallback for non-unity assemblies: try to make it look like a package. "My.Cool.Lib" -> "com.my.cool.lib" (Common convention) or just "my.cool.lib". However, usually referencing the exact assembly name works as a resource check in modern Unity if the assembly is within a package. But keeping "com." prefix is safer for package checks. If it looks like a package (contains dots), lowercase it.
		if (lower.Contains('.'))
		{
			// If it doesn't start with com/net/org, prepend com.
			if (!lower.StartsWith("com.") && !lower.StartsWith("net.") && !lower.StartsWith("org."))
			{
				return "com." + lower;
			}
			return lower;
		}

		return lower;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string DeriveDefineSymbol(string assemblyName)
	{
		if (assemblyName.Contains(":")) return "HAS_UNKNOWN_REF"; // 1. Remove "GUID:..." if present (rare in name fields but possible in raw data)

		var sb = new StringBuilder("HAS_"); // 2. Convert to snake_case upper. "Unity.Entities" -> "HAS_UNITY_ENTITIES", "MyLib" -> "HAS_MYLIB"

		foreach (var c in assemblyName)
		{
			if (c == '.') sb.Append('_');
			else if (char.IsLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
		}

		return sb.ToString();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string DetokenizeString(string input, string packageName, string displayName, string description, string authorName, string unityVersion)
	{
		if (string.IsNullOrEmpty(input)) return input;

		var asmName = GetAssemblyNameFromPackage(packageName);
		var asmShortName = GetShortName(asmName);
		var companyName = GetCompanyNameFromPackage(packageName);

		// Use provided values or fallback to derived values
		displayName = string.IsNullOrEmpty(displayName) ? GetDisplayNameFromPackage(packageName) : displayName;
		description = string.IsNullOrEmpty(description) ? $"Generated by {displayName}" : description;
		authorName = string.IsNullOrEmpty(authorName) ? companyName : authorName;
		unityVersion = string.IsNullOrEmpty(unityVersion) ? "2022.3" : unityVersion;

		// Standard detokenization
		return input
			.Replace("{{PACKAGE_NAME}}", packageName)
			.Replace("{{DISPLAY_NAME}}", displayName)
			.Replace("{{DESCRIPTION}}", description)
			.Replace("{{AUTHOR_NAME}}", authorName)
			.Replace("{{AUTHOR_EMAIL}}", string.Empty)
			.Replace("{{COMPANY_NAME}}", companyName)
			.Replace("{{VERSION}}", "1.0.0")
			.Replace("{{UNITY_VERSION}}", unityVersion)
			.Replace("{{ASM_NAME}}", asmName)
			.Replace("{{ASM_SHORT_NAME}}", asmShortName)
			.Replace("{{PACKAGE_PASCAL_NAME}}", asmName);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string DetokenizeString(string input, string packageName)
	{
		if (string.IsNullOrEmpty(input)) return input;

		var asmName = GetAssemblyNameFromPackage(packageName);
		var asmShortName = GetShortName(asmName);
		var companyName = GetCompanyNameFromPackage(packageName);
		var displayName = GetDisplayNameFromPackage(packageName);

		// Standard detokenization
		return input
			.Replace("{{PACKAGE_NAME}}", packageName)
			.Replace("{{DISPLAY_NAME}}", displayName)
			.Replace("{{DESCRIPTION}}", $"Generated by {displayName}")
			.Replace("{{AUTHOR_NAME}}", companyName)
			.Replace("{{AUTHOR_EMAIL}}", string.Empty)
			.Replace("{{COMPANY_NAME}}", companyName)
			.Replace("{{VERSION}}", "1.0.0")
			.Replace("{{UNITY_VERSION}}", "2022.3")
			.Replace("{{ASM_NAME}}", asmName)
			.Replace("{{ASM_SHORT_NAME}}", asmShortName)
			.Replace("{{PACKAGE_PASCAL_NAME}}", asmName);
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

			var sanitized = SanitizeToPascalCase(part);
			if (result.Length > 0) result.Append('.');
			result.Append(sanitized);
		}

		return result.Length > 0 ? result.ToString() : "MyNamespace";
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string GetShortName(string asmName)
	{
		var parts = asmName.Split('.');
		return parts.Length > 0 ? parts[^1] : asmName;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string GetCompanyNameFromPackage(string packageName)
	{
		var parts = packageName.Split('.');
		if (parts.Length >= 2 && parts[0] is "com" or "org" or "io" or "net")
		{
			return ToPascalCase(parts[1]);
		}
		return "YourCompany";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string GetDisplayNameFromPackage(string packageName)
	{
		var parts = packageName.Split('.');
		if (parts.Length >= 3)
		{
			var lastPart = ToPascalCase(parts[^1]);
			return $"{GetCompanyNameFromPackage(packageName)} {lastPart}";
		}
		return ToPascalCase(packageName);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string ToPascalCase(string input)
	{
		if (string.IsNullOrEmpty(input)) return input;
		var parts = input.Split('-', '_');
		var result = new StringBuilder();
		foreach (var part in parts)
		{
			if (string.IsNullOrEmpty(part)) continue;
			if (part.Length > 0)
				result.Append(char.ToUpperInvariant(part[0]));
			if (part.Length > 1)
				result.Append(part.Substring(1).ToLowerInvariant());
		}
		return result.Length > 0 ? result.ToString() : input;
	}
}
