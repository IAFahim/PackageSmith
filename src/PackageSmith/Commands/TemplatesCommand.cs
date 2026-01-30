using System.ComponentModel;
using PackageSmith.Core.Templates;
using Spectre.Console;
using Spectre.Console.Cli;

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
            .AddColumn("[yellow]Name[/]")
            .AddColumn("[yellow]Display Name[/]")
            .AddColumn("[yellow]Description[/]")
            .AddColumn("[yellow]Tags[/]");

        foreach (var template in templates)
        {
            var builtInMarker = template.BuiltIn ? " [dim](built-in)[/]" : "";
            table.AddRow(
                $"[cyan]{template.Name}[/]",
                $"[green]{template.DisplayName}[/]{builtInMarker}",
                template.Description,
                string.Join(", ", template.Tags.Select(t => $"[blue]{t}[/]"))
            );
        }

        AnsiConsole.Write(
            new Panel(table)
                .Header("[bold yellow]📦 Available Templates[/]")
                .BorderColor(Color.Yellow)
        );

        AnsiConsole.MarkupLine("\n[dim]Use 'pksmith templates info <name>' to see more details[/]");

        return 0;
    }

    private int ShowTemplateInfo(TemplateRegistry registry, Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.TemplateName))
        {
            AnsiConsole.MarkupLine("[red]Error: Template name is required for 'info' action[/]");
            AnsiConsole.MarkupLine("[dim]Usage: pksmith templates info <template-name>[/]");
            return 1;
        }

        var template = registry.GetTemplate(settings.TemplateName);
        if (template == null)
        {
            AnsiConsole.MarkupLine($"[red]Error: Template '{settings.TemplateName}' not found[/]");
            return 1;
        }

        var panel = new Panel(
            new Markup($@"[bold]{template.DisplayName}[/]

[dim]Name:[/] [cyan]{template.Name}[/]
[dim]Version:[/] {template.Version}
[dim]Author:[/] {template.Author}
[dim]Built-in:[/] {(template.BuiltIn ? "[green]Yes[/]" : "[yellow]No[/]")}

[bold yellow]Description:[/]
{template.Description}

[bold yellow]Tags:[/]
{string.Join(", ", template.Tags.Select(t => $"[blue]{t}[/]"))}

[bold yellow]Modules:[/]
{string.Join(", ", template.Modules.Select(m => $"[green]{m}[/]"))}

[bold yellow]Unity Version:[/]
{template.Dependencies.Unity}

{(template.Dependencies.Packages.Any() ? $@"[bold yellow]Package Dependencies:[/]
{string.Join("\n", template.Dependencies.Packages.Select(p => $"  • {p}"))}" : "")}
"))
            .Header($"[bold green]📦 {template.DisplayName}[/]")
            .BorderColor(Color.Green);

        AnsiConsole.Write(panel);

        if (template.AssemblyDependencies.Any())
        {
            AnsiConsole.MarkupLine("\n[bold yellow]Assembly Dependencies:[/]");
            foreach (var (module, deps) in template.AssemblyDependencies)
            {
                var depsStr = deps.Any() ? string.Join(", ", deps) : "[dim]none[/]";
                AnsiConsole.MarkupLine($"  [cyan]{module}[/] → {depsStr}");
            }
        }

        if (template.InternalsVisibleTo.Any())
        {
            AnsiConsole.MarkupLine("\n[bold yellow]InternalsVisibleTo Configuration:[/]");
            foreach (var (module, visibleTo) in template.InternalsVisibleTo)
            {
                var visibleStr = string.Join(", ", visibleTo.Select(v => $"[green]{v}[/]"));
                AnsiConsole.MarkupLine($"  [cyan]{module}[/] → {visibleStr}");
            }
        }

        AnsiConsole.MarkupLine($"\n[dim]Create package: pksmith new {template.Name}[/]");

        return 0;
    }

    private int PreviewTemplate(TemplateRegistry registry, Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.TemplateName))
        {
            AnsiConsole.MarkupLine("[red]Error: Template name is required for 'preview' action[/]");
            return 1;
        }

        var template = registry.GetTemplate(settings.TemplateName);
        if (template == null)
        {
            AnsiConsole.MarkupLine($"[red]Error: Template '{settings.TemplateName}' not found[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[bold green]📂 Package Structure Preview: {template.DisplayName}[/]\n");

        var tree = new Tree($"[yellow]com.company.package/[/]");
        
        foreach (var module in template.Modules)
        {
            var moduleNode = tree.AddNode($"[cyan]{module}/[/]");
            moduleNode.AddNode($"[dim]{module}.asmdef[/]");
            
            if (module == "Data")
            {
                moduleNode.AddNode("[dim]AssemblyInfo.cs[/]");
                moduleNode.AddNode("[dim]Components/[/]");
            }
            else if (module == "Authoring")
            {
                moduleNode.AddNode("[dim]AssemblyInfo.cs[/]");
                moduleNode.AddNode("[dim]Bakers/[/]");
            }
            else if (module == "Systems")
            {
                moduleNode.AddNode("[dim]Systems/[/]");
            }
            else if (module == "Tests")
            {
                moduleNode.AddNode("[dim]TestFixtures/[/]");
            }
        }

        tree.AddNode("[dim]package.json[/]");
        tree.AddNode("[dim]README.md[/]");
        tree.AddNode("[dim]CHANGELOG.md[/]");
        tree.AddNode("[dim]LICENSE.md[/]");

        AnsiConsole.Write(tree);

        return 0;
    }

    private int ShowUsage()
    {
        AnsiConsole.MarkupLine("[red]Invalid action[/]");
        AnsiConsole.MarkupLine("[yellow]Usage:[/]");
        AnsiConsole.MarkupLine("  pksmith templates list [--tag <TAG>] [--search <TERM>]");
        AnsiConsole.MarkupLine("  pksmith templates info <template-name>");
        AnsiConsole.MarkupLine("  pksmith templates preview <template-name>");
        return 1;
    }
}
