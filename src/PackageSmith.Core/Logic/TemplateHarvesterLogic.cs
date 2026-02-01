using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

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

	public static bool TryHarvest(string sourcePath, string outputTemplatePath, string sourcePackageName, out int processedFiles)
	{
		processedFiles = 0;
		if (!Directory.Exists(sourcePath)) return false;

		if (Directory.Exists(outputTemplatePath)) Directory.Delete(outputTemplatePath, true);
		Directory.CreateDirectory(outputTemplatePath);

		// Extract metadata from package.json
		var metadata = ExtractMetadata(sourcePath);
		var rootAsmName = ExtractRootAssemblyName(sourcePath);

		var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories);

		foreach (var file in files)
		{
			if (file.Contains("/.git/") || file.Contains("\\.git\\")) continue;

			var fileName = Path.GetFileName(file);

			if (IsIgnoredFile(fileName)) continue;

			var action = AnalyzeFile(file);

			if (action == FileAction.Keep || action == FileAction.Tokenize)
			{
				var relativePath = Path.GetRelativePath(sourcePath, file);
				var tokenizedPath = TokenizePath(relativePath, in metadata, rootAsmName);
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

		return true;
	}

	private enum FileAction { Drop, Keep, Tokenize }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static FileAction AnalyzeFile(string filePath)
	{
		var ext = Path.GetExtension(filePath).ToLowerInvariant();

		return ext switch
		{
			".cs" => FileAction.Tokenize,
			".asmdef" => FileAction.Tokenize,
			".asmref" => FileAction.Keep,
			".json" => FileAction.Tokenize,
			".md" => FileAction.Tokenize,
			".meta" => FileAction.Drop,
			".csproj" => FileAction.Drop,
			".sln" => FileAction.Drop,
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
			var doc = JsonDocument.Parse(content);
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

			if (root.TryGetProperty("author", out var authorProp) && authorProp.ValueKind == JsonValueKind.Object)
			{
				if (authorProp.TryGetProperty("name", out var authorNameProp))
					metadata.AuthorName = authorNameProp.GetString() ?? metadata.AuthorName;

				if (authorProp.TryGetProperty("email", out var authorEmailProp))
					metadata.AuthorEmail = authorEmailProp.GetString() ?? metadata.AuthorEmail;
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
		if (parts.Length >= 2 && parts[0] is "com" or "org" or "io" or "net")
		{
			return ToPascalCase(parts[1]);
		}
		return "{{COMPANY_NAME}}";
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsIgnoredFile(string fileName)
	{
		if (fileName.StartsWith(".")) return true;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string ExtractRootAssemblyName(string sourcePath)
	{
		try
		{
			var dirs = Directory.GetDirectories(sourcePath);
			foreach (var dir in dirs)
			{
				var asmdefs = Directory.GetFiles(dir, "*.asmdef");
				if (asmdefs.Length > 0)
				{
					var content = File.ReadAllText(asmdefs[0]);
					var doc = JsonDocument.Parse(content);
					if (doc.RootElement.TryGetProperty("name", out var nameProp))
						return nameProp.GetString() ?? string.Empty;
				}
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
		result = result.Replace(metadata.PackageName, "{{PACKAGE_NAME}}");
		result = result.Replace(metadata.DisplayName, "{{DISPLAY_NAME}}");
		result = result.Replace(metadata.Description, "{{DESCRIPTION}}");
		result = result.Replace(metadata.AuthorName, "{{AUTHOR_NAME}}");
		result = result.Replace(metadata.AuthorEmail, "{{AUTHOR_EMAIL}}");
		result = result.Replace(metadata.CompanyName, "{{COMPANY_NAME}}");
		result = result.Replace(metadata.Version, "{{VERSION}}");
		result = result.Replace(metadata.UnityVersion, "{{UNITY_VERSION}}");

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
			var doc = JsonDocument.Parse(json);
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
						writer.WriteEndObject();
						break;

					case "keywords":
						// Write empty array for keywords
						writer.WritePropertyName(name);
						writer.WriteStartArray();
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
			// Fallback to simple tokenization if JSON parsing fails
			var result = json.Replace(metadata.PackageName, "{{PACKAGE_NAME}}");
			result = result.Replace(metadata.DisplayName, "{{DISPLAY_NAME}}");
			result = result.Replace(metadata.Description, "{{DESCRIPTION}}");
			result = result.Replace(metadata.AuthorName, "{{AUTHOR_NAME}}");
			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string TokenizePath(string path, in PackageMetadata metadata, string rootAsmName)
	{
		var result = path.Replace(metadata.PackageName, "{{PACKAGE_NAME}}");

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
