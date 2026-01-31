using Spectre.Console;
using Spectre.Console.Rendering;
using PackageSmith.Core.Templates;
using PackageSmith.Core.Configuration;

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

        // Build enhanced choices with description
        var choices = new SelectionPrompt<TemplateMetadata>()
            .Title($"[{StyleManager.Primary.ToMarkup()}]Choose a template:[/]")
            .HighlightStyle(new Style(foreground: StyleManager.Primary))
            .PageSize(10)
            .UseConverter(t => $"{StyleManager.SymArrow} {t.DisplayName,-30} [{StyleManager.Tertiary.ToMarkup()}]{t.Description}[/]")
            .AddChoices(templates);

        var selected = AnsiConsole.Prompt(choices);

        // Show the structure preview after selection
        LayoutManager.PrintSection("Selected Template");
        AnsiConsole.MarkupLine($"[{StyleManager.Primary.ToMarkup()}]{StyleManager.SymArrow} {selected.DisplayName}[/]\n");
        AnsiConsole.MarkupLine($"[{StyleManager.Secondary.ToMarkup()}]{selected.Description}[/]\n");

        // Show file tree
        var packageName = $"com.example.{selected.Name.Replace("-", ".")}";
        var tree = new Tree($"[{StyleManager.Secondary.ToMarkup()}]{packageName}/[/]");

        // Standard files
        tree.AddNode($"[{StyleManager.Tertiary.ToMarkup()}]package.json[/]");
        tree.AddNode($"[{StyleManager.Tertiary.ToMarkup()}]README.md[/]");

        // Modules
        foreach (var mod in selected.Modules)
        {
            var folderName = mod;
            var isStandard = mod is "Runtime" or "Editor" or "Tests" or "Samples";

            if (!isStandard)
            {
                var prettyName = ToPascalCase(selected.Name);
                folderName = $"{prettyName}.{mod}";
            }

            var node = tree.AddNode($"[{StyleManager.Secondary.ToMarkup()}]{folderName}/[/]");
            node.AddNode($"[{StyleManager.Tertiary.ToMarkup()}]{folderName}.asmdef[/]");
        }

        AnsiConsole.Write(tree);
        AnsiConsole.WriteLine();

        return selected.Name;
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var parts = input.Split(new[] { '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join("", parts.Select(p => char.ToUpper(p[0]) + (p.Length > 1 ? p.Substring(1) : "")));
    }
}
