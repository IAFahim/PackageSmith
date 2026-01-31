#!/usr/bin/env dotnet-script
#load "src/PackageSmith.Data/Types/TemplateType.cs"
#load "src/PackageSmith.Data/Templates/TemplateManifest.cs"
#load "src/PackageSmith.Core/Logic/TemplateHarvesterLogic.cs"

using PackageSmith.Core.Logic;

var source = "/mnt/5f79a6c2-0764-4cd7-88b4-12dbd1b39909/com.bovinelabs.bridge";
var output = "/tmp/PackageSmith_Test_Output";
var packageName = "com.bovinelabs.bridge";

Console.WriteLine($"Harvesting {source}...");
Console.WriteLine($"To {output}");

if (TemplateHarvesterLogic.TryHarvest(source, output, packageName, out var count))
{
    Console.WriteLine($"SUCCESS: Harvested {count} files");

    // Show what we got
    var files = Directory.GetFiles(output, "*", SearchOption.AllDirectories);
    Console.WriteLine("\n=== HARVESTED FILES ===");
    foreach (var f in files.OrderBy(x => x))
    {
        var rel = Path.GetRelativePath(output, f);
        Console.WriteLine($"  {rel}");
    }
}
else
{
    Console.WriteLine("FAILED");
}
