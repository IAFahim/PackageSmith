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

        LayoutManager.PrintSection("Select Template");
        AnsiConsole.MarkupLine($"[{StyleManager.MutedColor.ToMarkup()}](Use up/down arrows, Enter to select)[/]");
        AnsiConsole.WriteLine();

        var choices = new SelectionPrompt<TemplateMetadata>()
            .Title($"[{StyleManager.CommandColor.ToMarkup()}]Choose a template:[/]")
            .HighlightStyle(StyleManager.Accent)
            .PageSize(10)
            .UseConverter(t => t.DisplayName)
            .AddChoices(templates);

        var selected = AnsiConsole.Prompt(choices);

        AnsiConsole.MarkupLine($"[{StyleManager.SuccessColor.ToMarkup()}]{StyleManager.IconSuccess} Selected: {selected.DisplayName}[/]\n");
        return selected.Name;
    }
}
