using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace PackageSmith.Core.Logic;

public static class TemplateGeneratorLogic
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGenerateFromTemplate(string templatePath, string outputPath, string packageName, out int processedFiles)
	{
		processedFiles = 0;
		if (!Directory.Exists(templatePath)) return false;

		if (Directory.Exists(outputPath)) Directory.Delete(outputPath, true);
		Directory.CreateDirectory(outputPath);

		var files = Directory.EnumerateFiles(templatePath, "*", SearchOption.AllDirectories);

		foreach (var file in files)
		{
			// Skip manifest files
			if (Path.GetFileName(file).StartsWith(".")) continue;

			var relativePath = Path.GetRelativePath(templatePath, file);
			var destFile = Path.Combine(outputPath, relativePath);

			Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

			// Read and detokenize content
			var content = File.ReadAllText(file);
			var detokenizedContent = DetokenizeString(content, packageName);

			File.WriteAllText(destFile, detokenizedContent);
			processedFiles++;
		}

		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string DetokenizeString(string input, string packageName)
	{
		if (string.IsNullOrEmpty(input)) return input;

		// Extract the "name" part from package name (e.g., "com.studio.tool" -> "Tool")
		var parts = packageName.Split('.');
		var shortName = parts.Length > 0 ? parts[^1] : packageName;
		var pascalName = char.ToUpper(shortName[0]) + shortName.Substring(1);

		var result = input
			.Replace("{{PACKAGE_NAME}}", packageName)
			.Replace("{{PACKAGE_SHORT_NAME}}", shortName)
			.Replace("{{PACKAGE_PASCAL_NAME}}", pascalName);

		return result;
	}
}
