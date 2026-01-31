using Spectre.Console;
using Spectre.Console.Rendering;
using PackageSmith.Core.Templates;
using PackageSmith.Core.Configuration;
using PackageSmith.Core.Generation;
using PackageSmith.Core.AssemblyDefinition;

namespace PackageSmith.UI;

public static class LivePreviewManager
{
    public static string? PromptTemplateWithPreview(TemplateRegistry registry, PackageSmithConfig config)
    {
        var templates = registry.Templates.Values
            .Where(t => t.BuiltIn)
            .OrderBy(t => t.DisplayName)
            .ToList();

        if (templates.Count == 0)
            return null;

        LayoutManager.PrintHeader("Select Template");
        AnsiConsole.MarkupLine($"[{StyleManager.Dim.ToMarkup()}]{StyleManager.SymInfo} Up/Down Navigate • Enter Select • Esc Cancel[/]");
        AnsiConsole.WriteLine();

        var choices = new SelectionPrompt<TemplateMetadata>()
            .Title($"[{StyleManager.Primary.ToMarkup()}]Choose a template:[/]")
            .HighlightStyle(new Style(foreground: StyleManager.Primary))
            .PageSize(10)
            .UseConverter(t => $"{StyleManager.SymArrow} {t.DisplayName}")
            .AddChoices(templates);

        var selected = AnsiConsole.Prompt(choices);
        return selected.Name;
    }
}
