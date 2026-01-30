using System.Text.Json;

var json = File.ReadAllText("/mnt/5f79a6c2-0764-4cd7-88b4-12dbd1b39909/ECSTimelineBrain/Packages/manifest.json");
Console.WriteLine($"JSON length: {json.Length}");

var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip
};

var doc = JsonDocument.Parse(json);
var root = doc.RootElement;
Console.WriteLine($"Has dependencies: {root.TryGetProperty("dependencies", out var deps)}");
if (deps.ValueKind == JsonValueKind.Object)
{
    var count = 0;
    foreach (var prop in deps.EnumerateObject())
    {
        count++;
    }
    Console.WriteLine($"Dependencies count: {count}");
}
