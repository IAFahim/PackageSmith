using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PackageSmith.Core.Logic;

public static class TemplateHarvesterLogic
{
	// Metadata extracted from package.json for tokenization
	private struct PackageMetadata
	{
		public string PackageName;
		public string DisplayName;
		public string Description;
		public string AuthorName;
		public string AuthorEmail;
		public string Version;
		public string UnityVersion;
		public string CompanyName;
	}

	private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
	{
		".git", ".vs", ".idea", ".vscode", "Library", "Temp", "Logs", "obj", "bin", "Build", "Builds", "UserSettings", "MemoryCaptures"
	};

	private static readonly HashSet<string> IgnoredExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".csproj", ".sln", ".suo", ".user", ".userprefs", ".pidb", ".booproj", ".unityproj", ".pdb", ".db"
	};

	public static bool TryHarvest(string sourcePath, string outputTemplatePath, string sourcePackageName, out int processedFiles, bool keepMeta = false)
	{
		processedFiles = 0;
		if (!Directory.Exists(sourcePath)) return false;

		// 1. Locate the effective root (handle nested folders, missing package.json, etc.)
		var rootPath = LocateEffectiveRoot(sourcePath);
		
		if (Directory.Exists(outputTemplatePath)) Directory.Delete(outputTemplatePath, true);
		Directory.CreateDirectory(outputTemplatePath);

		// 2. Extract or Infer Metadata
		var metadata = ExtractMetadata(rootPath);
		
		// Fallback: If metadata is empty/default, try to use the source path or argument
		if (metadata.PackageName == "{{PACKAGE_NAME}}")
		{
			// Try to guess from directory name if package.json was missing
			var dirName = new DirectoryInfo(rootPath).Name;
			metadata.PackageName = ToPackageName(dirName);
			metadata.DisplayName = ToDisplayName(dirName);
			metadata.CompanyName = DeriveCompanyName(metadata.PackageName);
		}

		var rootAsmName = ExtractRootAssemblyName(rootPath);

		// 3. Traverse and Process
		var files = Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories);

		foreach (var file in files)
		{
			if (ShouldSkip(file, rootPath)) continue;

			var action = AnalyzeFile(file, keepMeta);

			if (action == FileAction.Keep || action == FileAction.Tokenize)
			{
				var relativePath = Path.GetRelativePath(rootPath, file);
				
				// Smart Normalization: Fix "stupid" structures
				var normalizedPath = NormalizeStructure(relativePath);

				var tokenizedPath = TokenizePath(normalizedPath, in metadata, rootAsmName);
				var destFile = Path.Combine(outputTemplatePath, tokenizedPath);

				Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

				if (action == FileAction.Tokenize)
				{
					var content = File.ReadAllText(file);
					var tokenizedContent = TokenizeContent(content, in metadata, rootAsmName, file);
					File.WriteAllText(destFile, tokenizedContent);
				}
				else
				{
					File.Copy(file, destFile);
				}

				processedFiles++;
			}
		}

		// 4. Ensure package.json exists and is valid
		PostProcessPackageJson(outputTemplatePath, metadata);

		return true;
	}

	private static string LocateEffectiveRoot(string startPath)
	{
		// Priority 1: Has package.json
		if (File.Exists(Path.Combine(startPath, "package.json"))) return startPath;

		// Priority 2: Recursively look for package.json (max depth 2 to avoid scanning entire drives)
		var pkgFiles = Directory.GetFiles(startPath, "package.json", SearchOption.AllDirectories);
		if (pkgFiles.Length > 0)
		{
			// Pick the shallowest one
			return Path.GetDirectoryName(pkgFiles.OrderBy(f => f.Length).First())!;
		}

		// Priority 3: Has .asmdef
		var asmFiles = Directory.GetFiles(startPath, "*.asmdef", SearchOption.AllDirectories);
		if (asmFiles.Length > 0)
		{
			// If we found asmdefs, the root is likely their common ancestor or the start path.
			// If the user pointed to "MyFolder" containing "Runtime/X.asmdef", "MyFolder" is the root.
			return startPath;
		}

		return startPath;
	}

	private static string NormalizeStructure(string relativePath)
	{
		var parts = relativePath.Replace('\\', '/').Split('/');
		if (parts.Length == 0) return relativePath;

		// 1. Strip top-level "Assets" (common in Unity projects)
		if (parts[0].Equals("Assets", StringComparison.OrdinalIgnoreCase))
		{
			if (parts.Length == 1) return ""; // Ignore Assets folder itself
			parts = parts.Skip(1).ToArray();
		}

		// 2. Map "Scripts" -> "Runtime"
		if (parts.Length > 0 && parts[0].Equals("Scripts", StringComparison.OrdinalIgnoreCase))
		{
			parts[0] = "Runtime";
		}

		// 3. Move root source files to Runtime or Editor
		if (parts.Length == 1)
		{
			var ext = Path.GetExtension(parts[0]).ToLowerInvariant();
			if (ext == ".cs" || ext == ".asmdef")
			{
				if (parts[0].Contains("Editor", StringComparison.OrdinalIgnoreCase))
				{
					return "Editor/" + parts[0];
				}
				else
				{
					return "Runtime/" + parts[0];
				}
			}
		}

		return string.Join('/', parts);
	}

	private static void PostProcessPackageJson(string outputRoot, PackageMetadata metadata)
	{
		var path = Path.Combine(outputRoot, "package.json");
		string jsonContent;

		if (!File.Exists(path))
		{
			// Create a minimal package.json
			jsonContent = """
			{
			  "name": "{{PACKAGE_NAME}}",
			  "version": "{{VERSION}}",
			  "displayName": "{{DISPLAY_NAME}}",
			  "description": "{{DESCRIPTION}}",
			  "unity": "{{UNITY_VERSION}}",
			  "author": {
			    "name": "{{AUTHOR_NAME}}",
			    "email": "{{AUTHOR_EMAIL}}"
			  }
			}
			""";
		}
		else
		{
			jsonContent = File.ReadAllText(path);
		}

		// Inject Samples if present in directory but missing in JSON
		var samplesDir = Path.Combine(outputRoot, "Samples~");
		if (Directory.Exists(samplesDir))
		{
			try
			{
				using var doc = JsonDocument.Parse(jsonContent);
				if (!doc.RootElement.TryGetProperty("samples", out _))
				{
					// Found Samples~ but no samples entry in JSON. Inject it.
					var subDirs = Directory.GetDirectories(samplesDir);
					if (subDirs.Length > 0)
					{
						var samplesList = new List<object>();
						foreach (var dir in subDirs)
						{
							var dirName = Path.GetFileName(dir);
							samplesList.Add(new
							{
								displayName = ToDisplayName(dirName),
								description = $"Samples for {dirName}",
								path = $"Samples~/{dirName}"
							});
						}
						
						// Deserialize to dictionary to modify
						var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
						if (dict != null)
						{
							dict["samples"] = samplesList;
							jsonContent = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
						}
					}
				}
			}
			catch { /* Ignore invalid JSON */ }
		}

		File.WriteAllText(path, jsonContent);
	}

	private static bool ShouldSkip(string filePath, string rootPath)
	{
		var fileName = Path.GetFileName(filePath);
		if (fileName.StartsWith(".")) return true; // .DS_Store, etc.

		// Check ignored extensions
		if (IgnoredExtensions.Contains(Path.GetExtension(filePath))) return true;

		// Check ignored directories
		var relative = Path.GetRelativePath(rootPath, filePath);
		var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		foreach (var part in parts)
		{
			if (IgnoredDirectories.Contains(part)) return true;
		}

		return false;
	}

	private enum FileAction { Drop, Keep, Tokenize }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static FileAction AnalyzeFile(string filePath, bool keepMeta)
	{
		var ext = Path.GetExtension(filePath).ToLowerInvariant();

		return ext switch
		{
			".cs" => FileAction.Tokenize,
			".asmdef" => FileAction.Tokenize,
			".asmref" => FileAction.Keep,
			".json" => FileAction.Tokenize,
			".md" => FileAction.Tokenize,
			".meta" => keepMeta ? FileAction.Keep : FileAction.Drop,
			".yml" => FileAction.Tokenize,
			".yaml" => FileAction.Tokenize,
			".txt" => FileAction.Tokenize,
			".xml" => FileAction.Tokenize,
			".uss" => FileAction.Tokenize,
			".uxml" => FileAction.Tokenize,
			".shader" => FileAction.Tokenize,
			".hlsl" => FileAction.Tokenize,
			".cginc" => FileAction.Tokenize,
			_ => FileAction.Keep
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static PackageMetadata ExtractMetadata(string sourcePath)
	{
		var metadata = new PackageMetadata
		{
			PackageName = "{{PACKAGE_NAME}}",
			DisplayName = "{{DISPLAY_NAME}}",
			Description = "{{DESCRIPTION}}",
			AuthorName = "{{AUTHOR_NAME}}",
			AuthorEmail = "{{AUTHOR_EMAIL}}",
			Version = "{{VERSION}}",
			UnityVersion = "{{UNITY_VERSION}}",
			CompanyName = "{{COMPANY_NAME}}"
		};

		try
		{
			var packageJsonPath = Path.Combine(sourcePath, "package.json");
			if (!File.Exists(packageJsonPath)) return metadata;

			var content = File.ReadAllText(packageJsonPath);
			using var doc = JsonDocument.Parse(content);
			var root = doc.RootElement;

			if (root.TryGetProperty("name", out var nameProp))
				metadata.PackageName = nameProp.GetString() ?? metadata.PackageName;

			if (root.TryGetProperty("displayName", out var displayProp))
				metadata.DisplayName = displayProp.GetString() ?? metadata.DisplayName;

			if (root.TryGetProperty("description", out var descProp))
				metadata.Description = descProp.GetString() ?? metadata.Description;

			if (root.TryGetProperty("version", out var versionProp))
				metadata.Version = versionProp.GetString() ?? metadata.Version;

			if (root.TryGetProperty("unity", out var unityProp))
				metadata.UnityVersion = unityProp.GetString() ?? metadata.UnityVersion;

			if (root.TryGetProperty("author", out var authorProp))
			{
				if (authorProp.ValueKind == JsonValueKind.Object)
				{
					if (authorProp.TryGetProperty("name", out var authorNameProp))
						metadata.AuthorName = authorNameProp.GetString() ?? metadata.AuthorName;

					if (authorProp.TryGetProperty("email", out var authorEmailProp))
						metadata.AuthorEmail = authorEmailProp.GetString() ?? metadata.AuthorEmail;
				}
				else if (authorProp.ValueKind == JsonValueKind.String)
				{
					metadata.AuthorName = authorProp.GetString() ?? metadata.AuthorName;
				}
			}

			// Derive company name from package name if not set
			if (metadata.AuthorName == "{{AUTHOR_NAME}}")
			{
				metadata.CompanyName = DeriveCompanyName(metadata.PackageName);
			}
			else
			{
				metadata.CompanyName = metadata.AuthorName;
			}
		}
		catch { /* Ignore parsing errors */ }

		return metadata;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string DeriveCompanyName(string packageName)
	{
		// Extract company name from package name: com.company.product -> Company
		var parts = packageName.Split('.');
		if (parts.Length >= 2 && (parts[0] == "com" || parts[0] == "org" || parts[0] == "io" || parts[0] == "net"))
		{
			return ToPascalCase(parts[1]);
		}
		return "{{COMPANY_NAME}}";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string ToPascalCase(string input)
	{
		if (string.IsNullOrEmpty(input)) return input;
		var parts = input.Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
		var result = new StringBuilder();
		foreach (var part in parts)
		{
			if (part.Length > 0)
				result.Append(char.ToUpperInvariant(part[0]));
			if (part.Length > 1)
				result.Append(part.Substring(1).ToLowerInvariant());
		}
		return result.Length > 0 ? result.ToString() : input;
	}

	private static string ToPackageName(string dirName)
	{
		// Convert "My Super Tool" to "com.company.my-super-tool"
		// But we don't know the company. So just "my-super-tool"
		// The tokenizer will assume {{PACKAGE_NAME}} so we assume this is the source value to replace.
		// Actually, if we are inferring metadata because package.json is missing, 
		// we want to use the *directory name* as the "source value" that we seek and replace with {{PACKAGE_NAME}}.
		// Actually, ExtractMetadata returns the values *found in the source*.
		// If we set metadata.PackageName = "MyFolder", then TokenizeContent will replace "MyFolder" with "{{PACKAGE_NAME}}".
		return dirName;
	}

	private static string ToDisplayName(string dirName)
	{
		return dirName; 
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string ExtractRootAssemblyName(string sourcePath)
	{
		try
		{
			// Find ALL asmdefs
			var asmdefs = Directory.GetFiles(sourcePath, "*.asmdef", SearchOption.AllDirectories);
			if (asmdefs.Length == 0) return string.Empty;

			var candidates = new List<string>();

			foreach (var asm in asmdefs)
			{
				try 
				{
					var content = File.ReadAllText(asm);
					using var doc = JsonDocument.Parse(content);
					if (doc.RootElement.TryGetProperty("name", out var nameProp))
					{
						var name = nameProp.GetString();
						if (!string.IsNullOrEmpty(name))
						{
							candidates.Add(name);
						}
					}
				}
				catch {}
			}

			// Pick the shortest name as it is likely the root (e.g. "MyLib" vs "MyLib.Tests")
			if (candidates.Count > 0)
			{
				return candidates.OrderBy(x => x.Length).ThenBy(x => x).First();
			}
		}
		catch { /* Ignore */ }

		return string.Empty;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string TokenizeContent(string input, in PackageMetadata metadata, string rootAsmName, string filePath)
	{
		if (string.IsNullOrEmpty(input)) return input;

		var result = input;

		// Package.json special handling - clear arrays and objects
		if (filePath.EndsWith("package.json", StringComparison.OrdinalIgnoreCase))
		{
			result = TokenizePackageJson(result, in metadata);
			return result;
		}

		// Standard tokenization for all other files
		// We replace the *found* metadata values with the *placeholders*
		
		// Only replace if the metadata value is NOT the placeholder itself (which happens if we failed to extract)
		if (metadata.PackageName != "{{PACKAGE_NAME}}") result = result.Replace(metadata.PackageName, "{{PACKAGE_NAME}}");
		if (metadata.DisplayName != "{{DISPLAY_NAME}}") result = result.Replace(metadata.DisplayName, "{{DISPLAY_NAME}}");
		// Description might be generic, careful replacing short strings? 
		// If description is "Cool tool", replacing it is fine.
		if (metadata.Description != "{{DESCRIPTION}}" && metadata.Description.Length > 5) result = result.Replace(metadata.Description, "{{DESCRIPTION}}");
		
		if (metadata.AuthorName != "{{AUTHOR_NAME}}") result = result.Replace(metadata.AuthorName, "{{AUTHOR_NAME}}");
		if (metadata.AuthorEmail != "{{AUTHOR_EMAIL}}") result = result.Replace(metadata.AuthorEmail, "{{AUTHOR_EMAIL}}");
		if (metadata.CompanyName != "{{COMPANY_NAME}}") result = result.Replace(metadata.CompanyName, "{{COMPANY_NAME}}");
		
		// Version is tricky. "1.0.0" is common. We might replace it in unrelated places.
		// Use regex for version replacement to be safer? Or just trust context?
		// For now, let's skip version replacement in arbitrary files to avoid breaking "v1.0.0" strings in code/comments unrelated to package version.
		// But in AssemblyInfo or package.json it matters.
		// Let's rely on TokenizePackageJson for that, and maybe specific AssemblyInfo files.
		
		if (!string.IsNullOrEmpty(rootAsmName))
		{
			result = result.Replace(rootAsmName, "{{ASM_NAME}}");

			var parts = rootAsmName.Split('.');
			if (parts.Length > 0)
			{
				var shortName = parts[^1];
				result = result.Replace(shortName, "{{ASM_SHORT_NAME}}");
			}
		}

		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string TokenizePackageJson(string json, in PackageMetadata metadata)
	{
		try
		{
			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;

			using var stream = new MemoryStream();
			using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

			writer.WriteStartObject();

			foreach (var prop in root.EnumerateObject())
			{
				var name = prop.Name.ToString();

				switch (name)
				{
					case "name":
						writer.WriteString(name, "{{PACKAGE_NAME}}");
						break;

					case "displayName":
						writer.WriteString(name, "{{DISPLAY_NAME}}");
						break;

					case "description":
						writer.WriteString(name, "{{DESCRIPTION}}");
						break;

					case "version":
						writer.WriteString(name, "{{VERSION}}");
						break;

					case "unity":
						writer.WriteString(name, "{{UNITY_VERSION}}");
						break;

					case "author":
						writer.WritePropertyName(name);
						writer.WriteStartObject();
						writer.WriteString("name", "{{AUTHOR_NAME}}");
						writer.WriteString("email", "{{AUTHOR_EMAIL}}");
						writer.WriteEndObject();
						break;

					case "keywords":
						// Write empty array for keywords
						writer.WritePropertyName(name);
						writer.WriteStartArray();
						writer.WriteEndArray();
						break;

					case "samples":
						writer.WritePropertyName(name);
						writer.WriteStartArray();
						if (prop.Value.ValueKind == JsonValueKind.Array)
						{
							foreach (var sample in prop.Value.EnumerateArray())
							{
								if (sample.ValueKind == JsonValueKind.Object)
								{
									writer.WriteStartObject();
									foreach (var sProp in sample.EnumerateObject())
									{
										var sVal = sProp.Value.ToString();
										// Light tokenization for sample properties
										if (metadata.PackageName != "{{PACKAGE_NAME}}") sVal = sVal.Replace(metadata.PackageName, "{{PACKAGE_NAME}}");
										writer.WriteString(sProp.Name, sVal);
									}
									writer.WriteEndObject();
								}
							}
						}
						writer.WriteEndArray();
						break;

					case "dependencies":
						// Keep dependencies as-is (they will be template)
						prop.WriteTo(writer);
						break;

					default:
						prop.WriteTo(writer);
						break;
				}
			}

			writer.WriteEndObject();
			writer.Flush();

			return Encoding.UTF8.GetString(stream.ToArray());
		}
		catch
		{
			// Fallback
			return json;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string TokenizePath(string path, in PackageMetadata metadata, string rootAsmName)
	{
		var result = path;
		if (metadata.PackageName != "{{PACKAGE_NAME}}") result = result.Replace(metadata.PackageName, "{{PACKAGE_NAME}}");

		if (!string.IsNullOrEmpty(rootAsmName))
		{
			result = result.Replace(rootAsmName, "{{ASM_NAME}}");

			var parts = rootAsmName.Split('.');
			if (parts.Length > 0)
			{
				var shortName = parts[^1];
				result = result.Replace(shortName, "{{ASM_SHORT_NAME}}");
			}
		}

		return result;
	}
}