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

		var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories);

		foreach (var file in files)
		{
			// Skip files in ignored directories
			if (file.Contains("/.git/") || file.Contains("\\.git\\")) continue;

			var fileName = Path.GetFileName(file);

			if (IsIgnoredFile(fileName)) continue;

			var action = AnalyzeFile(file);

			if (action == FileAction.Keep || action == FileAction.Tokenize)
			{
				var relativePath = Path.GetRelativePath(sourcePath, file);
				var tokenizedPath = TokenizeString(relativePath, sourcePackageName);
				var destFile = Path.Combine(outputTemplatePath, tokenizedPath);

				Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

				if (action == FileAction.Tokenize)
				{
					var content = File.ReadAllText(file);
					var tokenizedContent = TokenizeString(content, sourcePackageName);
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
		if (fileName.StartsWith(".")) return true; // .git, .DS_Store, etc
		return false;
	}

	private static bool IsIgnoredDirectory(string dirPath)
	{
		var dirName = Path.GetFileName(dirPath);
		return dirName.Equals(".git", StringComparison.OrdinalIgnoreCase);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string TokenizeString(string input, string sourcePackageName)
	{
		if (string.IsNullOrEmpty(input)) return input;

		var result = input.Replace(sourcePackageName, "{{PACKAGE_NAME}}");

		return result;
	}
}
