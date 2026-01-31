using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using PackageSmith.Core.Configuration;
using PackageSmith.Core.Generation;
using PackageSmith.Core.AssemblyDefinition;
using PackageSmith.Core.Dependencies;
using PackageSmith.Core.Templates;
using PackageSmith.Core.AI;
using PackageSmith.UI;

namespace PackageSmith.Commands;

public sealed class NewCommand : Command<NewCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[name]")]
        [Description("Package name in reverse domain notation (e.g., com.company.feature)")]
        public string? PackageName { get; init; }

        [CommandOption("-n|--name")]
        [Description("Display name for the package")]
        public string? DisplayName { get; init; }

        [CommandOption("-d|--description")]
        [Description("Package description")]
        public string? Description { get; init; }

        [CommandOption("-o|--output")]
        [Description("Output directory path")]
        public string? OutputPath { get; init; }

        [CommandOption("-m|--modules")]
        [Description("Comma-separated modules (Runtime,Editor,Tests,Samples)")]
        public string? Modules { get; init; }

        [CommandOption("--no-wizard")]
        [Description("Skip interactive wizard, use defaults")]
        public bool NoWizard { get; init; }

        [CommandOption("-y|--yes")]
        [Description("Skip confirmation prompt")]
        public bool SkipConfirmation { get; init; }

        [CommandOption("--ecs")]
        [Description("Enable ECS mode with Unity.Entities, Burst, Collections, Mathematics")]
        public bool EnableEcs { get; init; }

        [CommandOption("--sub-assemblies")]
        [Description("Enable sub-assembly structure (Core,Data,Authoring,Runtime,Systems,Debug)")]
        public bool EnableSubAssemblies { get; init; }

        [CommandOption("--depends")]
        [Description("Package dependencies (comma-separated: Unity.Entities,com.unity.logging)")]
        public string? Dependencies { get; init; }

        [CommandOption("--template")]
        [Description("Code template: MonoBehaviour, ScriptableObject, SystemBase, IComponentData, Authoring, EcsFull")]
        public string? Template { get; init; }

        [CommandOption("--dry-run")]
        [Description("Preview without writing files")]
        public bool DryRun { get; init; }

        [CommandOption("--json")]
        [Description("Output as JSON for AI consumption (use with --dry-run)")]
        public bool JsonOutput { get; init; }
    }

    private readonly IConfigService _configService;
    private readonly IPackageGenerator _generator;
    private readonly IFileSystemWriter _writer;
    private readonly TemplateRegistry _templateRegistry;

    public NewCommand()
    {
        _configService = new ConfigService();
        _generator = new PackageGenerator();
        _writer = new FileSystemWriter();
        _templateRegistry = new TemplateRegistry();
        _templateRegistry.LoadTemplates();
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (!_configService.ConfigExists())
        {
            AnsiConsole.MarkupLine($"[{StyleManager.WarningColor.ToMarkup()}]{StyleManager.IconWarning} No configuration found. Running setup wizard...[/]\n");
            var settingsCmd = new SettingsCommand();
            settingsCmd.Execute(context, new SettingsCommand.Settings());
        }

        _configService.TryLoadConfig(out var config);

        var template = CreateTemplate(settings, in config);

        if (!_generator.TryGenerate(in template, in config, out var layout))
        {
            AnsiConsole.MarkupLine($"[{StyleManager.ErrorColor.ToMarkup()}]{StyleManager.IconError} Failed to generate package layout[/]");
            return 1;
        }

        if (settings.JsonOutput)
        {
            var packageContext = PackageContext.FromTemplate(in template, in config);
            var dryRun = new PackageDryRun(template.PackageName, layout.Files, layout.Directories, packageContext);
            AnsiConsole.Write(dryRun.ToJson());
            return 0;
        }

        if (settings.DryRun)
        {
            ShowPreview(in template, in layout);
            AnsiConsole.MarkupLine($"\n[{StyleManager.WarningColor.ToMarkup()}]{StyleManager.IconInfo} Dry run completed. No files were written[/]");
            return 0;
        }

        ShowPreview(in template, in layout);

        if (!settings.SkipConfirmation && !ConfirmCreation())
        {
            AnsiConsole.MarkupLine($"[{StyleManager.MutedColor.ToMarkup()}]{StyleManager.IconInfo} Operation cancelled[/]");
            return 0;
        }

        ShowProfileStatusBar(in config);

        var writeSuccess = LiveGenerationManager.GenerateWithLivePreview(layout, template.OutputPath, template.PackageName);

        if (writeSuccess)
        {
            // Write .package-context file
            WriteContextFile(in template, in config);

            // Git init if enabled
            if (!settings.NoWizard)
            {
                GitInit(template);
            }

            LayoutManager.PrintFooter();
            AnsiConsole.MarkupLine($"[{StyleManager.SuccessColor.ToMarkup()}]{StyleManager.IconSuccess} Package created at:[/] [{StyleManager.PathColor.ToMarkup()}]{template.OutputPath}/{template.PackageName}[/]");
            return 0;
        }

        AnsiConsole.MarkupLine($"[{StyleManager.ErrorColor.ToMarkup()}]{StyleManager.IconError} Failed to write package to disk[/]");
        return 1;
    }

    private static void WriteContextFile(in PackageTemplate template, in PackageSmithConfig config)
    {
        var context = PackageContext.FromTemplate(in template, in config);
        var contextPath = Path.Combine(template.OutputPath, template.PackageName, ".package-context.md");
        File.WriteAllText(contextPath, context.ToMarkdown());
    }

    private static void GitInit(in PackageTemplate template)
    {
        var packagePath = Path.Combine(template.OutputPath, template.PackageName);

        try
        {
            var gitIgnorePath = Path.Combine(packagePath, ".gitignore");
            File.WriteAllText(gitIgnorePath, GetUnityPackageGitIgnore());

            AnsiConsole.MarkupLine("\n[gray]Hint: Run 'git init' in the package folder to initialize git.[/]");
        }
        catch
        {
            // Ignore errors
        }
    }

    private static string GetUnityPackageGitIgnore()
    {
        return """
        # Unity Package .gitignore

        # OS generated files
        .DS_Store
        .DS_Store?
        ._*
        .Spotlight-V100
        .Trashes
        ehthumbs.db
        Thumbs.db

        # Unity
        *.csproj
        *.sln
        *.suo
        *.user
        *.userprefs
        *.unityproj
        .idea/

        # Build results
        [Bb]in/
        [Bb]obj/
        [Ll]og/
        [Ll]ogs/
        [Ll]ibrary/
        [Tt]emp/
        [Oo]bj/
        [Bb]uild/
        [Bb]uilds/
        [Dd]ebug*/
        [Rr]elease*/
        [xX]86/
        [xX]64/
        [Bb]uilt/
        [Aa]ssets/[Aa]ssets/
        [Ll]ibs/
        [Ss]cripts/

        # NuGet
        packages.config
        """;
    }

    private PackageTemplate CreateTemplate(in Settings settings, in PackageSmithConfig config)
    {
        // Step 1: Select template type
        string? selectedTemplateName = null;
        if (!settings.NoWizard)
        {
            selectedTemplateName = PromptTemplateSelection();
        }

        var name = settings.PackageName;

        if (string.IsNullOrWhiteSpace(name) || !settings.NoWizard)
        {
            name = PromptPackageName();
        }

        var modules = ParseModules(settings.Modules);
        if (modules == PackageModule.None && !settings.NoWizard)
        {
            modules = PromptModules();
        }

        // Apply template defaults if selected
        var ecsPreset = settings.EnableEcs ? EcsPreset.Full : EcsPreset.None;
        var subAssemblies = settings.EnableSubAssemblies ? SubAssemblyType.All : SubAssemblyType.None;

        if (selectedTemplateName != null)
        {
            var templateMeta = _templateRegistry.GetTemplate(selectedTemplateName);
            if (templateMeta != null)
            {
                // Apply template settings
                if (selectedTemplateName == "ecs-simple")
                {
                    ecsPreset = EcsPreset.Simple;
                }
                else if (selectedTemplateName == "ecs-modular")
                {
                    ecsPreset = EcsPreset.Full;
                    subAssemblies = SubAssemblyType.All;
                }
            }
        }

        var dependencies = ParseDependencies(settings.Dependencies);
        var template = ParseTemplate(settings.Template);

        return new PackageTemplate
        {
            PackageName = name!,
            DisplayName = settings.DisplayName ?? ExtractDisplayName(name),
            Description = settings.Description ?? string.Empty,
            SelectedModules = modules,
            OutputPath = settings.OutputPath ?? Directory.GetCurrentDirectory(),
            CompanyName = config.CompanyName,
            UnityVersion = config.DefaultUnityVersion,
            EcsPreset = ecsPreset,
            SubAssemblies = subAssemblies,
            EnableSubAssemblies = subAssemblies != SubAssemblyType.None,
            Dependencies = dependencies,
            SelectedTemplate = template
        };
    }

    private string PromptTemplateSelection()
    {
        return LivePreviewManager.PromptTemplateWithPreview(_templateRegistry, _configService.LoadConfigOrDefault()) ?? "basic";
    }

    private static string PromptPackageName()
    {
        return PromptManager.PromptPackageName();
    }

    private static PackageModule PromptModules()
    {
        var modules = new Dictionary<PackageModule, (string Label, string? Description)>
        {
            { PackageModule.Runtime, ($"{StyleManager.IconCode} Runtime", "Runtime code and components") },
            { PackageModule.Editor, ($"{StyleManager.IconConfig} Editor", "Editor-only tools and utilities") },
            { PackageModule.Tests, ($"{StyleManager.IconTest} Tests", "Test assemblies and test runners") },
            { PackageModule.Samples, ($"{StyleManager.IconFolder} Samples", "Example scenes and usage samples") }
        };

        var selected = PromptManager.PromptMultipleChoices("Select Modules", modules);

        var result = PackageModule.None;
        foreach (var module in selected)
        {
            result |= module;
        }

        return result;
    }

    private static TemplateType ParseTemplate(string? template)
    {
        if (string.IsNullOrWhiteSpace(template)) return TemplateType.None;

        return template.ToLowerInvariant() switch
        {
            "monobehaviour" => TemplateType.MonoBehaviour,
            "scriptableobject" => TemplateType.ScriptableObject,
            "editorwindow" => TemplateType.EditorWindow,
            "systembase" => TemplateType.SystemBase,
            "isystem" => TemplateType.ISystem,
            "icomponentdata" => TemplateType.IComponentData,
            "isharedcomponentdata" => TemplateType.ISharedComponentData,
            "baker" => TemplateType.Baker,
            "authoring" => TemplateType.Authoring,
            "ecsfull" => TemplateType.EcsFull,
            "ecsstandard" => TemplateType.EcsStandard,
            "standard" => TemplateType.Standard,
            _ => TemplateType.None
        };
    }

    private static PackageDependency[] ParseDependencies(string? dependencies)
    {
        if (string.IsNullOrWhiteSpace(dependencies)) return Array.Empty<PackageDependency>();

        var parts = dependencies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var resolver = new DependencyResolver();

        var results = new List<PackageDependency>();
        foreach (var part in parts)
        {
            if (resolver.TryResolveDependency(part, out var dep))
            {
                results.Add(dep);
            }
            else
            {
                // Treat as direct package reference
                results.Add(new PackageDependency(part, null, DependencyType.UnityPackage));
            }
        }

        return results.ToArray();
    }

    private static PackageModule ParseModules(string? modules)
    {
        if (string.IsNullOrWhiteSpace(modules)) return PackageModule.None;

        var result = PackageModule.None;
        var parts = modules.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (Enum.TryParse<PackageModule>(part, ignoreCase: true, out var module))
            {
                result |= module;
            }
        }

        return result;
    }

    private static string ExtractDisplayName(string? packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName)) return "New Package";

        var parts = packageName.Split('.');
        var lastSegment = parts[^1];

        // Convert to PascalCase
        return char.ToUpper(lastSegment[0]) + (lastSegment.Length > 1 ? lastSegment.Substring(1) : string.Empty);
    }

    private static void ShowPreview(in PackageTemplate template, in PackageLayout layout)
    {
        LayoutManager.PrintSection("Package Structure Preview");

        var packageName = template.PackageName;
        var outputPath = template.OutputPath;
        var packagePath = Path.Combine(outputPath, packageName);

        var root = new Tree($"[{StyleManager.CommandColor.ToMarkup()}]{StyleManager.IconPackage} {packageName}/[/]");

        // Add directories
        foreach (var dir in layout.Directories.Skip(1))
        {
            var relativePath = GetRelativePath(dir.Path, packagePath);
            var node = root.AddNode($"[{StyleManager.PathColor.ToMarkup()}]{StyleManager.IconFolder} {relativePath}/[/]");

            // Add files in this directory
            var filesInDir = layout.Files.Where(f => Path.GetDirectoryName(f.Path) == dir.Path);
            foreach (var file in filesInDir)
            {
                var fileName = Path.GetFileName(file.Path);
                var icon = GetFileIcon(fileName);
                node.AddNode($"[{StyleManager.InfoColor.ToMarkup()}]{icon} {fileName}[/]");
            }
        }

        // Add root files
        var rootFiles = layout.Files.Where(f =>
        {
            var dir = Path.GetDirectoryName(f.Path);
            return dir == packagePath;
        });

        foreach (var file in rootFiles)
        {
            var fileName = Path.GetFileName(file.Path);
            var icon = GetFileIcon(fileName);
            root.AddNode($"[{StyleManager.InfoColor.ToMarkup()}]{icon} {fileName}[/]");
        }

        AnsiConsole.Write(root);
        AnsiConsole.WriteLine();
    }

    private static string GetFileIcon(string fileName)
    {
        return Path.GetExtension(fileName) switch
        {
            ".asmdef" => StyleManager.IconCode,
            ".cs" => StyleManager.IconCode,
            ".md" => StyleManager.IconInfo,
            ".json" => StyleManager.IconConfig,
            _ => StyleManager.IconDependency
        };
    }

    private static string GetRelativePath(string fullPath, string basePath)
    {
        if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        return fullPath;
    }

    private static bool ConfirmCreation()
    {
        return PromptManager.PromptConfirmation("Create this package?", defaultValue: true);
    }

    private static void ShowProfileStatusBar(in PackageSmithConfig config)
    {
        AnsiConsole.WriteLine();
        var rule = new Rule(StyleManager.ProfileStatusBar(config))
        {
            Style = StyleManager.Muted,
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
    }
}
