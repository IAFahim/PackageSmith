#!/usr/bin/env dotnet-script
#r "nuget: Spectre.Console, 0.49.1"
#r "nuget: System.Text.Json, 9.0.1"

using System.Diagnostics;

// Configuration
var appDll = "src/PackageSmith.App/bin/Debug/net9.0/PackageSmith.App.dll";
var tempRoot = Path.Combine(Directory.GetCurrentDirectory(), "temp_harvester_test");
var sourceDir = Path.Combine(tempRoot, "SourcePkg");
var templateName = "TestHarvesterTemplate";

Console.WriteLine($"[Test] Setting up environment in {tempRoot}...");

if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
Directory.CreateDirectory(sourceDir);

// 1. Create Mock Package
var pkgName = "com.test.harvester";
File.WriteAllText(Path.Combine(sourceDir, "package.json"), 
    $@"{{ ""name"": ""{pkgName}"", ""version"": ""1.2.3"", ""displayName"": ""Harvester Test"" }}");

Directory.CreateDirectory(Path.Combine(sourceDir, "Runtime"));
File.WriteAllText(Path.Combine(sourceDir, "Runtime", "Script.cs"), "public class Script {}");
File.WriteAllText(Path.Combine(sourceDir, "Runtime", $"{pkgName}.asmdef"), 
    $@"{{ ""name"": ""{pkgName}"", ""references"": [], ""versionDefines"": [{{ ""name"": ""Unity.Burst"", ""define"": ""BURST"" }}] }}");

var samplesDir = Path.Combine(sourceDir, "Samples~", "MySample");
Directory.CreateDirectory(samplesDir);
File.WriteAllText(Path.Combine(samplesDir, "sample.txt"), "Sample Data");

Directory.CreateDirectory(Path.Combine(sourceDir, "Documentation~"));
File.WriteAllText(Path.Combine(sourceDir, "Documentation~", "README.md"), "# Docs");

// 2. Run Harvest Logic via CLI
Console.WriteLine("[Test] Running Harvest CLI...");
var psi = new ProcessStartInfo
{
    FileName = "dotnet",
    Arguments = $"{appDll} harvest \"{sourceDir}\" {templateName} --keep-meta",
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false
};

var p = Process.Start(psi);
p.WaitForExit();

Console.WriteLine(p.StandardOutput.ReadToEnd());
var err = p.StandardError.ReadToEnd();
if (!string.IsNullOrEmpty(err)) Console.WriteLine("STDERR: " + err);

if (p.ExitCode != 0)
{
    Console.WriteLine("[Fail] CLI exited with error code " + p.ExitCode);
    Environment.Exit(1);
}

// 3. Verify Output
// Output location defaults to LocalAppData/PackageSmith/Templates
var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
var templateDir = Path.Combine(localAppData, "PackageSmith", "Templates", templateName);

Console.WriteLine($"[Test] Verifying output at {templateDir}...");

if (!Directory.Exists(templateDir))
{
    Console.WriteLine("[Fail] Output directory not found.");
    Environment.Exit(1);
}

var jsonPath = Path.Combine(templateDir, "package.json");
if (!File.Exists(jsonPath)) 
{
    Console.WriteLine("[Fail] package.json not found in template.");
    Environment.Exit(1);
}

var json = File.ReadAllText(jsonPath);
bool passed = true;

if (!json.Contains("\"samples\": [")) { Console.WriteLine("[Fail] Missing samples entry"); passed = false; }
if (!json.Contains("Samples~/MySample")) { Console.WriteLine("[Fail] Missing sample path"); passed = false; }

if (!File.Exists(Path.Combine(templateDir, "Runtime", "{{ASM_NAME}}.asmdef"))) 
{ 
    // Check for PACKAGE_NAME alternative
    if (!File.Exists(Path.Combine(templateDir, "Runtime", "{{PACKAGE_NAME}}.asmdef")))
    {
            Console.WriteLine("[Fail] Asmdef not found or not tokenized correctly"); passed = false; 
    }
}

if (!Directory.Exists(Path.Combine(templateDir, "Samples~"))) { Console.WriteLine("[Fail] Samples~ missing"); passed = false; }

if (passed) 
{
    Console.WriteLine("[Test] ALL CHECKS PASSED");
    // Cleanup on success
    // Directory.Delete(tempRoot, true); 
}
else 
{
    Console.WriteLine("[Test] SOME CHECKS FAILED");
    Environment.Exit(1);
}
