using System.Text.Json;
using PackageSmith.Core.Generation;
using PackageSmith.Core.Configuration;

namespace PackageSmith.Core.AI;

public readonly struct PackageDryRun
{
    public readonly string PackageName;
    public readonly VirtualFile[] Files;
    public readonly VirtualDirectory[] Directories;
    public readonly PackageContext Context;

    public PackageDryRun(string packageName, VirtualFile[] files, VirtualDirectory[] directories, PackageContext context)
    {
        PackageName = packageName;
        Files = files;
        Directories = directories;
        Context = context;
    }

    public readonly string ToJson()
    {
        var files = Files.Select(f => new
        {
            path = f.Path,
            content = f.Content
        }).ToArray();

        var directories = Directories.Select(d => new
        {
            path = d.Path
        }).ToArray();

        var result = new
        {
            packageName = PackageName,
            directories = directories,
            files = files,
            context = JsonSerializer.Deserialize<JsonElement>(Context.ToJson())
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
