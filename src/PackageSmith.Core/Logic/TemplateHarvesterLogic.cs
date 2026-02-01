using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace PackageSmith.Core.Logic;

public static class TemplateHarvesterLogic
{
	private const int HeaderScanLimit = 4096;

	public static bool TryHarvest(string sourcePath, string outputTemplatePath, string sourcePackageName, out int processedFiles)
	{
		processedFiles = 0;
		if (!Directory.Exists(sourcePath)) return false;

		if (Directory.Exists(outputTemplatePath)) Directory.Delete(outputTemplatePath, true);
		Directory.CreateDirectory(outputTemplatePath);

		// Extract root assembly name from first .asmdef
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
				var tokenizedPath = TokenizeString(relativePath, sourcePackageName, rootAsmName);
				var destFile = Path.Combine(outputTemplatePath, tokenizedPath);

				Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

				if (action == FileAction.Tokenize)
				{
					var content = File.ReadAllText(file);
					var tokenizedContent = TokenizeString(content, sourcePackageName, rootAsmName);
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
			".cs" => AnalyzeCSharpFile(filePath),
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

	private static FileAction AnalyzeCSharpFile(string path)
	{
		try
		{
			using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
			var buffer = new byte[HeaderScanLimit];
			var bytesRead = fs.Read(buffer, 0, HeaderScanLimit);

			var content = Encoding.UTF8.GetString(buffer, 0, bytesRead);

			if (content.Contains("[assembly: DisableAutoTypeRegistration]", StringComparison.Ordinal) ||
				content.Contains("[assembly: InternalsVisibleTo", StringComparison.Ordinal))
			{
				return FileAction.Tokenize;
			}
		}
		catch
		{
			// Ignore read errors
		}

		return FileAction.Drop;
	}

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
					var doc = System.Text.Json.JsonDocument.Parse(content);
					if (doc.RootElement.TryGetProperty("name", out var nameProp))
						return nameProp.GetString() ?? string.Empty;
				}
			}
		}
		catch { /* Ignore */ }

		return string.Empty;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string TokenizeString(string input, string sourcePackageName, string rootAsmName)
	{
		if (string.IsNullOrEmpty(input)) return input;

		var result = input.Replace(sourcePackageName, "{{PACKAGE_NAME}}");

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
