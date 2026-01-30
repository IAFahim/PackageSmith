using System.Text.Json;

namespace PackageSmith.Core.Templates;

public class TemplateRegistry
{
    private readonly Dictionary<string, TemplateMetadata> _templates = new();
    private readonly List<string> _templatePaths = new();

    public IReadOnlyDictionary<string, TemplateMetadata> Templates => _templates;

    public TemplateRegistry()
    {
        // Add default template paths
        var userTemplatesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".pksmith",
            "templates"
        );
        _templatePaths.Add(userTemplatesPath);

        // Built-in templates will be registered directly
        RegisterBuiltInTemplates();
    }

    public void AddTemplatePath(string path)
    {
        if (!_templatePaths.Contains(path))
        {
            _templatePaths.Add(path);
        }
    }

    public void LoadTemplates()
    {
        foreach (var templatePath in _templatePaths)
        {
            if (!Directory.Exists(templatePath))
            {
                continue;
            }

            var templateDirs = Directory.GetDirectories(templatePath);
            foreach (var templateDir in templateDirs)
            {
                LoadTemplateFromDirectory(templateDir);
            }
        }
    }

    private void LoadTemplateFromDirectory(string templateDir)
    {
        var metadataPath = Path.Combine(templateDir, "template.json");
        if (!File.Exists(metadataPath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(metadataPath);
            var metadata = JsonSerializer.Deserialize<TemplateMetadata>(json);
            
            if (metadata != null)
            {
                _templates[metadata.Name] = metadata;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load template from {templateDir}: {ex.Message}");
        }
    }

    public void RegisterTemplate(TemplateMetadata metadata)
    {
        _templates[metadata.Name] = metadata;
    }

    public TemplateMetadata? GetTemplate(string name)
    {
        return _templates.TryGetValue(name, out var template) ? template : null;
    }

    public IEnumerable<TemplateMetadata> SearchTemplates(string? searchTerm = null, IEnumerable<string>? tags = null)
    {
        var results = _templates.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            results = results.Where(t =>
                t.Name.ToLowerInvariant().Contains(term) ||
                t.DisplayName.ToLowerInvariant().Contains(term) ||
                t.Description.ToLowerInvariant().Contains(term)
            );
        }

        if (tags != null && tags.Any())
        {
            var tagSet = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
            results = results.Where(t => t.Tags.Any(tag => tagSet.Contains(tag)));
        }

        return results.OrderBy(t => t.DisplayName);
    }

    private void RegisterBuiltInTemplates()
    {
        // Register basic template
        RegisterTemplate(new TemplateMetadata
        {
            Name = "basic",
            DisplayName = "Basic Package",
            Description = "Simple Unity package with Runtime and Editor modules",
            Tags = new List<string> { "basic", "simple", "beginner" },
            Modules = new List<string> { "Runtime", "Editor", "Tests" },
            BuiltIn = true,
            Variables = new Dictionary<string, TemplateVariable>
            {
                ["packageName"] = new TemplateVariable
                {
                    Type = "string",
                    Description = "Package identifier (e.g., com.company.package)",
                    Required = true,
                    Pattern = @"^[a-z][a-z0-9]*\.[a-z][a-z0-9]*\.[a-z][a-z0-9]*$"
                },
                ["displayName"] = new TemplateVariable
                {
                    Type = "string",
                    Description = "Display name for the package",
                    Required = true
                },
                ["author"] = new TemplateVariable
                {
                    Type = "string",
                    Description = "Package author",
                    Required = false
                }
            }
        });

        // Register ECS simple template
        RegisterTemplate(new TemplateMetadata
        {
            Name = "ecs-simple",
            DisplayName = "ECS Simple",
            Description = "Single-assembly ECS package with components and systems",
            Tags = new List<string> { "ecs", "simple", "entities" },
            Modules = new List<string> { "Runtime", "Editor", "Tests" },
            BuiltIn = true,
            Dependencies = new TemplateDependencies
            {
                Unity = "2022.3",
                Packages = new List<string>
                {
                    "com.unity.entities@1.0.0",
                    "com.unity.burst@1.8.0"
                }
            }
        });

        // Register ECS modular template
        RegisterTemplate(new TemplateMetadata
        {
            Name = "ecs-modular",
            DisplayName = "ECS Modular Architecture",
            Description = "Sophisticated multi-assembly ECS structure with Data/Authoring/Runtime/Systems/Editor/Debug modules",
            Tags = new List<string> { "ecs", "modular", "advanced", "production" },
            Modules = new List<string> { "Data", "Authoring", "Runtime", "Systems", "Editor", "Debug", "Tests" },
            BuiltIn = true,
            Dependencies = new TemplateDependencies
            {
                Unity = "2022.3",
                Packages = new List<string>
                {
                    "com.unity.entities@1.0.0",
                    "com.unity.burst@1.8.0",
                    "com.unity.mathematics@1.3.0"
                }
            },
            AssemblyDependencies = new Dictionary<string, List<string>>
            {
                ["Data"] = new List<string>(),
                ["Authoring"] = new List<string> { "Data" },
                ["Runtime"] = new List<string> { "Data" },
                ["Systems"] = new List<string> { "Data", "Runtime" },
                ["Editor"] = new List<string> { "Data", "Authoring", "Runtime", "Systems" },
                ["Debug"] = new List<string> { "Data", "Runtime", "Systems" },
                ["Tests"] = new List<string> { "Data", "Runtime", "Systems" }
            },
            InternalsVisibleTo = new Dictionary<string, List<string>>
            {
                ["Data"] = new List<string> { "Authoring", "Runtime", "Editor", "Tests" },
                ["Authoring"] = new List<string> { "Editor", "Tests" },
                ["Runtime"] = new List<string> { "Systems", "Editor", "Debug", "Tests" },
                ["Systems"] = new List<string> { "Editor", "Debug", "Tests" }
            }
        });
    }
}
