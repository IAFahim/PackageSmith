using System;
using System.IO;
using System.Linq;
using PackageSmith.Core.Logic;

namespace PackageSmith.Console;

public static class Program
{
	public static int Main(string[] args)
	{
		var sourcePath = "/mnt/5f79a6c2-0764-4cd7-88b4-12dbd1b39909/com.bovinelabs.bridge";
		var templatePath = "/tmp/PackageSmith_Harvested_Bridge";
		var outputPath = "/tmp/PackageSmith_Recreated_Bridge";
		var packageName = "com.test.newbridge";

		System.Console.WriteLine("◆ PackageSmith Template Cycle Test\n");
		System.Console.WriteLine("Step 1: Harvesting template...");
		System.Console.WriteLine($"  Source: {sourcePath}");
		System.Console.WriteLine($"  Template: {templatePath}");

		if (!TemplateHarvesterLogic.TryHarvest(sourcePath, templatePath, "com.bovinelabs.bridge", out var harvestedCount))
		{
			System.Console.WriteLine("  FAILED: Harvest failed");
			return 1;
		}

		System.Console.WriteLine($"  SUCCESS: Harvested {harvestedCount} files\n");

		System.Console.WriteLine("Step 2: Generating from template...");
		System.Console.WriteLine($"  Template: {templatePath}");
		System.Console.WriteLine($"  Output: {outputPath}");
		System.Console.WriteLine($"  New Package: {packageName}");

		if (!TemplateGeneratorLogic.TryGenerateFromTemplate(templatePath, outputPath, packageName, out var generatedCount))
		{
			System.Console.WriteLine("  FAILED: Generation failed");
			return 1;
		}

		System.Console.WriteLine($"  SUCCESS: Generated {generatedCount} files\n");

		// Compare structures
		System.Console.WriteLine("Step 3: Verification...");
		System.Console.WriteLine("\n  Original structure:");
		foreach (var f in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
			.Where(f => !f.Contains("/.git/") && !f.Contains("\\.git\\") && !Path.GetFileName(f).StartsWith("."))
			.OrderBy(f => f)
			.Take(15))
		{
			var rel = Path.GetRelativePath(sourcePath, f);
			System.Console.WriteLine($"    {rel}");
		}

		System.Console.WriteLine($"\n  Generated structure ({packageName}):");
		foreach (var f in Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories)
			.Where(f => !Path.GetFileName(f).StartsWith("."))
			.OrderBy(f => f))
		{
			var rel = Path.GetRelativePath(outputPath, f);
			System.Console.WriteLine($"    {rel}");
		}

		// Verify package.json was detokenized correctly
		var packageJsonPath = Path.Combine(outputPath, "package.json");
		if (File.Exists(packageJsonPath))
		{
			System.Console.WriteLine("\n  Generated package.json:");
			var content = File.ReadAllText(packageJsonPath);
			System.Console.WriteLine(content);
		}

		System.Console.WriteLine("\n✓ Template cycle complete!");
		return 0;
	}
}
