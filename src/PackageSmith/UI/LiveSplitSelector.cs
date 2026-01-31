using Spectre.Console;
using Spectre.Console.Rendering;
using PackageSmith.Core.Templates;
using PackageSmith.Core.Configuration;

namespace PackageSmith.UI;

public static class LiveSplitSelector
{
    public static string? SelectTemplate(TemplateRegistry registry, PackageSmithConfig config)
    {
        var templates = registry.Templates.Values
            .Where(t => t.BuiltIn)
            .OrderBy(t => t.DisplayName)
            .ToList();

        if (templates.Count == 0) return null;

        AnsiConsole.Cursor.Hide();
        string? result = null;
        int index = 0;

        try
        {
            // Removed VerticalOverflow.Ellipsis to prevent top-cropping
            AnsiConsole.Live(CreateLayout(templates, index))
                .AutoClear(false)
                .Start(ctx =>
                {
                    while (true)
                    {
                        ctx.UpdateTarget(CreateLayout(templates, index));

                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true).Key;

                            if (key == ConsoleKey.UpArrow)
                            {
                                index = (index - 1 + templates.Count) % templates.Count;
                            }
                            else if (key == ConsoleKey.DownArrow)
                            {
                                index = (index + 1) % templates.Count;
                            }
                            else if (key == ConsoleKey.Enter)
                            {
                                result = templates[index].Name;
                                break;
                            }
                            else if (key == ConsoleKey.Escape)
                            {
                                break;
                            }
                        }
                        Thread.Sleep(20);
                    }
                });
        }
        finally
        {
            AnsiConsole.Cursor.Show();
        }

        return result;
    }

    private static IRenderable CreateLayout(List<TemplateMetadata> templates, int index)
    {
        // Grid Configuration
        var grid = new Grid();
        grid.AddColumn(new GridColumn().Width(35).NoWrap()); // Fixed Menu Width
        grid.AddColumn(new GridColumn().Width(2));           // Padding
        grid.AddColumn(new GridColumn());                    // Flexible Preview

        // Fixed height to prevent jumping/cropping
        var fixedHeight = 16;

        var menuPanel = CreateMenuPanel(templates, index, fixedHeight);
        var previewPanel = CreatePreviewPanel(templates[index], fixedHeight);

        grid.AddRow(menuPanel, Text.Empty, previewPanel);

        // Footer
        var footer = new Markup($"[{StyleManager.Tertiary.ToMarkup()}]{StyleManager.SymInfo} Up/Down Navigate • Enter Select • Esc Cancel[/]");

        return new Rows(
            grid,
            new Padder(footer, new Padding(0, 1, 0, 0))
        );
    }

    private static Panel CreateMenuPanel(List<TemplateMetadata> templates, int index, int height)
    {
        var menuItems = new List<IRenderable>();

        for (int i = 0; i < templates.Count; i++)
        {
            var t = templates[i];
            if (i == index)
            {
                menuItems.Add(new Markup($"[{StyleManager.Primary.ToMarkup()}]{StyleManager.SymArrow} [bold]{t.DisplayName}[/][/]"));
            }
            else
            {
                menuItems.Add(new Markup($"[{StyleManager.Tertiary.ToMarkup()}]  {t.DisplayName}[/]"));
            }
        }

        return new Panel(new Rows(menuItems))
            .Header($"[{StyleManager.Primary.ToMarkup()}]Templates[/]")
            .HeaderAlignment(Justify.Left)
            .BorderStyle(new Style(StyleManager.Primary))
            .Border(BoxBorder.Rounded)
            .Padding(1, 0, 1, 0);
    }

    private static Panel CreatePreviewPanel(TemplateMetadata current, int height)
    {
        // Calculate Package Name
        var rawName = current.Name.Replace("-", ".");
        var prettyName = ToPascalCase(current.Name);
        var packageName = $"com.example.{rawName}";

        // Build Tree
        var tree = new Tree($"[{StyleManager.Secondary.ToMarkup()}]{packageName}/[/]");
        tree.Style = new Style(StyleManager.Tertiary);

        // Standard Files
        tree.AddNode($"[{StyleManager.Tertiary.ToMarkup()}]package.json[/]");
        tree.AddNode($"[{StyleManager.Tertiary.ToMarkup()}]README.md[/]");

        // Modules
        foreach (var mod in current.Modules)
        {
            string folderName = mod;
            bool isStandardFolder = mod is "Runtime" or "Editor" or "Tests" or "Samples";
            if (!isStandardFolder) folderName = $"{prettyName}.{mod}";

            var node = tree.AddNode($"[{StyleManager.Secondary.ToMarkup()}]{folderName}/[/]");
            node.AddNode($"[{StyleManager.Tertiary.ToMarkup()}]{folderName}.asmdef[/]");
        }

        var content = new Rows(
            new Markup($"\n[{StyleManager.Primary.ToMarkup()}][bold]{current.DisplayName}[/][/]\n"),
            new Markup($"[{StyleManager.Secondary.ToMarkup()}]{current.Description}[/]\n"),
            new Markup(""),
            new Markup($"[{StyleManager.Tertiary.ToMarkup()}]Structure:[/]\n"),
            tree
        );

        return new Panel(content)
            .Header($"[{StyleManager.Primary.ToMarkup()}]Preview[/]")
            .HeaderAlignment(Justify.Left)
            .BorderStyle(new Style(StyleManager.Tertiary))
            .Border(BoxBorder.Rounded)
            .Padding(1, 0, 1, 0);
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var parts = input.Split(new[] { '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join("", parts.Select(p => char.ToUpper(p[0]) + (p.Length > 1 ? p.Substring(1) : "")));
    }
}
