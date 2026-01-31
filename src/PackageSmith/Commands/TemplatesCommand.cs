using System.ComponentModel;
using PackageSmith.Core.Templates;
using Spectre.Console;
using Spectre.Console.Cli;
using PackageSmith.UI;

namespace PackageSmith.Commands;

[Description("Manage package templates")]
public class TemplatesCommand : Command<TemplatesCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[action]")]
        [Description("Action to perform: list, info, preview")]
        public string Action { get; set; } = "list";

        [CommandArgument(1, "[template]")]
        [Description("Template name (for info/preview actions)")]
        public string? TemplateName { get; set; }

        [CommandOption("--tag <TAG>")]
        [Description("Filter templates by tag")]
        public string[]? Tags { get; set; }

        [CommandOption("--search <TERM>")]
        [Description("Search templates by name or description")]
        public string? SearchTerm { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var registry = new TemplateRegistry();
        registry.LoadTemplates();

        return settings.Action.ToLowerInvariant() switch
        {
            "list" => ListTemplates(registry, settings),
            "info" => ShowTemplateInfo(registry, settings),
            "preview" => PreviewTemplate(registry, settings),
            _ => ShowUsage()
        };
    }

    private int ListTemplates(TemplateRegistry registry, Settings settings)
    {
        var templates = registry.SearchTemplates(settings.SearchTerm, settings.Tags);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(StyleManager.Primary)
            .AddColumn($"[{StyleManager.Primary.ToMarkup()}]Name[/]")
            .AddColumn($"[{StyleManager.Primary.ToMarkup()}]Display Name[/]")
            .AddColumn($"[{StyleManager.Primary.ToMarkup()}]Description[/]")
            .AddColumn($"[{StyleManager.Primary.ToMarkup()}]Tags[/]");

        foreach (var template in templates)
        {
            var builtInMarker = template.BuiltIn ? $"[{StyleManager.Tertiary.ToMarkup()}](built-in)[/]" : "";
            table.AddRow(
                $"[{StyleManager.Primary.ToMarkup()}]{template.Name}[/]",
                $"[{StyleManager.SuccessColor.ToMarkup()}]{template.DisplayName}[/]{builtInMarker}",
                template.Description,
                string.Join(", ", template.Tags.Select(t => $"[{StyleManager.InfoColor.ToMarkup()}]{t}[/]"))
            );
        }

        AnsiConsole.Write(
            new Panel(table)
                .Header($"[{StyleManager.Primary.ToMarkup()}]{StyleManager.SymBullet} Available Templates[/]")
                .BorderStyle(new Style(StyleManager.Primary))
                .Border(BoxBorder.Rounded)
        );

        AnsiConsole.MarkupLine($"\n[{StyleManager.Tertiary.ToMarkup()}]{StyleManager.SymInfo} Use 'pksmith templates info <name>' to see more details[/]");

        return 0;
    }

    private int ShowTemplateInfo(TemplateRegistry registry, Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.TemplateName))
        {
            AnsiConsole.MarkupLine($"[{StyleManager.ErrorColor.ToMarkup()}]{StyleManager.SymError} Template name is required for 'info' action[/]");
            AnsiConsole.MarkupLine($"[{StyleManager.Tertiary.ToMarkup()}]Usage: pksmith templates info <template-name>[/]");
            return 1;
        }

        var template = registry.GetTemplate(settings.TemplateName);
        if (template == null)
        {
            AnsiConsole.MarkupLine($"[{StyleManager.ErrorColor.ToMarkup()}]{StyleManager.SymError} Template '{settings.TemplateName}' not found[/]");
            return 1;
        }

        var panel = new Panel(
            new Markup($@"[{StyleManager.Content.ToMarkup()}]{template.DisplayName}[/]

[{StyleManager.Tertiary.ToMarkup()}]Name:[/] [{StyleManager.Primary.ToMarkup()}]{template.Name}[/]
[{StyleManager.Tertiary.ToMarkup()}]Version:[/] {template.Version}
[{StyleManager.Tertiary.ToMarkup()}]Author:[/] {template.Author}
[{StyleManager.Tertiary.ToMarkup()}]Built-in:[/] {(template.BuiltIn ? $"[{StyleManager.SuccessColor.ToMarkup()}]Yes[/]" : $"[{StyleManager.WarningColor.ToMarkup()}]No[/]")}

[{StyleManager.Primary.ToMarkup()}]Description:[/]
{template.Description}

[{StyleManager.Primary.ToMarkup()}]Tags:[/]
{string.Join(", ", template.Tags.Select(t => $"[{StyleManager.InfoColor.ToMarkup()}]{t}[/]"))}

[{StyleManager.Primary.ToMarkup()}]Modules:[/]
{string.Join(", ", template.Modules.Select(m => $"[{StyleManager.SuccessColor.ToMarkup()}]{m}[/]"))}

[{StyleManager.Primary.ToMarkup()}]Unity Version:[/]
{template.Dependencies.Unity}

{(template.Dependencies.Packages.Any() ? $@"[{StyleManager.Primary.ToMarkup()}]Package Dependencies:[/]
{string.Join("\n", template.Dependencies.Packages.Select(p => $"  {StyleManager.SymBullet} {p}"))}" : "")}
"))
            .Header($"[{StyleManager.SuccessColor.ToMarkup()}]{StyleManager.SymBullet} {template.DisplayName}[/]")
            .BorderStyle(new Style(StyleManager.SuccessColor))
            .Border(BoxBorder.Rounded);

        AnsiConsole.Write(panel);

        if (template.AssemblyDependencies.Any())
        {
            AnsiConsole.MarkupLine($"\n[{StyleManager.Primary.ToMarkup()}]Assembly Dependencies:[/]");
            foreach (var (module, deps) in template.AssemblyDependencies)
            {
                var depsStr = deps.Any() ? string.Join(", ", deps) : $"[{StyleManager.Tertiary.ToMarkup()}]none[/]";
                AnsiConsole.MarkupLine($"  [{StyleManager.Primary.ToMarkup()}]{module}[/] {StyleManager.SymArrow} {depsStr}");
            }
        }

        if (template.InternalsVisibleTo.Any())
        {
            AnsiConsole.MarkupLine($"\n[{StyleManager.Primary.ToMarkup()}]InternalsVisibleTo Configuration:[/]");
            foreach (var (module, visibleTo) in template.InternalsVisibleTo)
            {
                var visibleStr = string.Join(", ", visibleTo.Select(v => $"[{StyleManager.SuccessColor.ToMarkup()}]{v}[/]"));
                AnsiConsole.MarkupLine($"  [{StyleManager.Primary.ToMarkup()}]{module}[/] {StyleManager.SymArrow} {visibleStr}");
            }
        }

        AnsiConsole.MarkupLine($"\n[{StyleManager.Tertiary.ToMarkup()}]{StyleManager.SymInfo} Create package: pksmith new {template.Name}[/]");

        return 0;
    }

    private int PreviewTemplate(TemplateRegistry registry, Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.TemplateName))
        {
            AnsiConsole.MarkupLine($"[{StyleManager.ErrorColor.ToMarkup()}]{StyleManager.SymError} Template name is required for 'preview' action[/]");
            return 1;
        }

        var template = registry.GetTemplate(settings.TemplateName);
        if (template == null)
        {
            AnsiConsole.MarkupLine($"[{StyleManager.ErrorColor.ToMarkup()}]{StyleManager.SymError} Template '{settings.TemplateName}' not found[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[{StyleManager.SuccessColor.ToMarkup()}]{StyleManager.SymBullet} Package Structure Preview: {template.DisplayName}[/]\n");

        var tree = new Tree($"[{StyleManager.Primary.ToMarkup()}]com.company.package/[/]");

        foreach (var module in template.Modules)
        {
            var moduleNode = tree.AddNode($"[{StyleManager.Primary.ToMarkup()}]{module}/[/]");
            moduleNode.AddNode($"[{StyleManager.Tertiary.ToMarkup()}]{module}.asmdef[/]");

            if (module == "Data")
            {
                moduleNode.AddNode($"[{StyleManager.Tertiary.ToMarkup()}]AssemblyInfo.cs[/]");
                moduleNode.AddNode($"[{StyleManager.Tertiary.ToMarkup()}]Components/[/]");
            }
            else if (module == "Authoring")
            {
                moduleNode.AddNode($"[{StyleManager.Tertiary.ToMarkup()}]AssemblyInfo.cs[/]");
                moduleNode.AddNode($"[{StyleManager.Tertiary.ToMarkup()}]Bakers/[/]");
            }
            else if (module == "Systems")
            {
                moduleNode.AddNode($"[{StyleManager.Tertiary.ToMarkup()}]Systems/[/]");
            }
            else if (module == "Tests")
            {
                moduleNode.AddNode($"[{StyleManager.Tertiary.ToMarkup()}]TestFixtures/[/]");
            }
        }

        tree.AddNode($"[{StyleManager.Tertiary.ToMarkup()}]package.json[/]");
        tree.AddNode($"[{StyleManager.Tertiary.ToMarkup()}]README.md[/]");
        tree.AddNode($"[{StyleManager.Tertiary.ToMarkup()}]CHANGELOG.md[/]");
        tree.AddNode($"[{StyleManager.Tertiary.ToMarkup()}]LICENSE.md[/]");

        AnsiConsole.Write(tree);

        return 0;
    }

    private int ShowUsage()
    {
        AnsiConsole.MarkupLine($"[{StyleManager.WarningColor.ToMarkup()}]{StyleManager.SymWarning} Invalid action[/]");
        AnsiConsole.MarkupLine($"[{StyleManager.Tertiary.ToMarkup()}]Usage:[/]");
        AnsiConsole.MarkupLine($"  [{StyleManager.Primary.ToMarkup()}]pksmith templates list[/] [{StyleManager.Tertiary.ToMarkup()}]\\[--tag TAG] \\[--search TERM][/]");
        AnsiConsole.MarkupLine($"  [{StyleManager.Primary.ToMarkup()}]pksmith templates info <template-name>[/]");
        AnsiConsole.MarkupLine($"  [{StyleManager.Primary.ToMarkup()}]pksmith templates preview <template-name>[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[{StyleManager.Tertiary.ToMarkup()}]Examples:[/]");
        AnsiConsole.MarkupLine($"  pksmith templates list --tag ecs");
        AnsiConsole.MarkupLine($"  pksmith templates info ecs-modular");
        AnsiConsole.MarkupLine($"  pksmith templates preview basic");
        return 1;
    }
}
