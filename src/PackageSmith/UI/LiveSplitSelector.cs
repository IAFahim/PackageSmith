using Spectre.Console;
using Spectre.Console.Rendering;
using PackageSmith.Core.Templates;
using PackageSmith.Core.Configuration;
using PackageSmith.Core.Generation;
using PackageSmith.Core.AssemblyDefinition;

namespace PackageSmith.UI;

public static class LiveSplitSelector
{
    public static string? SelectTemplate(TemplateRegistry registry, PackageSmithConfig config)
    {
        var templates = registry.Templates.Values
            .Where(t => t.BuiltIn)
            .OrderBy(t => t.DisplayName)
            .ToList();

        if (templates.Count == 0)
            return null;

        var selectedIndex = 0;

        AnsiConsole.Cursor.Hide();
        try
        {
            AnsiConsole.Live(CreateDisplay(templates, selectedIndex, config))
                .Start(ctx =>
                {
                    while (true)
                    {
                        var key = Console.ReadKey(true);

                        switch (key.Key)
                        {
                            case ConsoleKey.UpArrow:
                                selectedIndex = (selectedIndex - 1 + templates.Count) % templates.Count;
                                ctx.UpdateTarget(CreateDisplay(templates, selectedIndex, config));
                                break;
                            case ConsoleKey.DownArrow:
                                selectedIndex = (selectedIndex + 1) % templates.Count;
                                ctx.UpdateTarget(CreateDisplay(templates, selectedIndex, config));
                                break;
                            case ConsoleKey.Enter:
                                return templates[selectedIndex].Name;
                            case ConsoleKey.Escape:
                                return null;
                        }
                    }
                });
        }
        finally
        {
            AnsiConsole.Cursor.Show();
        }

        return null; // Should not reach here
    }

    private static IRenderable CreateDisplay(List<TemplateMetadata> templates, int selectedIndex, PackageSmithConfig config)
    {
        var layout = new Layout("Root")
            .SplitColumns(
                new Layout("Left").Size(30),
                new Layout("Right").Size(50)
            );

        var menu = CreateMenuPanel(templates, selectedIndex);
        var preview = CreatePreviewPanel(templates[selectedIndex], config);

        layout["Left"].Update(menu);
        layout["Right"].Update(preview);

        var footer = new Rows(
            layout,
            new Markup($"\n[{StyleManager.Dim.ToMarkup()}]{StyleManager.SymInfo} Up/Down Navigate • Enter Select • Esc Cancel[/]\n")
        );

        return footer;
    }

    private static Panel CreateMenuPanel(List<TemplateMetadata> templates, int selectedIndex)
    {
        var rows = new List<IRenderable>();

        for (int i = 0; i < templates.Count; i++)
        {
            var t = templates[i];
            var isSelected = i == selectedIndex;

            if (isSelected)
            {
                var bar = new Markup($"[{StyleManager.Primary.ToMarkup()}]{StyleManager.SymArrow} {t.DisplayName}[/]");
                rows.Add(bar);
            }
            else
            {
                var text = new Markup($"[{StyleManager.Dim.ToMarkup()}]  {t.DisplayName}[/]");
                rows.Add(text);
            }
        }

        var column = new Rows(rows);

        return new Panel(column)
            .Header($"[{StyleManager.Primary.ToMarkup()}]Select Template[/]")
            .BorderStyle(new Style(StyleManager.Primary))
            .Border(BoxBorder.Rounded)
            .Padding(1, 0, 1, 0);
    }

    private static Panel CreatePreviewPanel(TemplateMetadata template, PackageSmithConfig config)
    {
        var content = new Rows(
            new Markup($"\n[{StyleManager.Primary.ToMarkup()}]{template.DisplayName}[/]\n"),
            new Markup($"[{StyleManager.Content.ToMarkup()}]{template.Description}[/]\n"),
            new Markup($"\n[{StyleManager.Dim.ToMarkup()}]Structure:[/]\n"),
            CreateFileTree(template),
            new Markup($"\n")
        );

        return new Panel(content)
            .Header($"[{StyleManager.Primary.ToMarkup()}]Preview[/]")
            .BorderStyle(new Style(StyleManager.Dim))
            .Border(BoxBorder.Rounded)
            .Padding(1, 0, 1, 0);
    }

    private static Tree CreateFileTree(TemplateMetadata template)
    {
        var packageName = $"com.example.{template.Name}";
        var asmdefRoot = NamespaceGenerator.GetAsmDefRootFromPackageName(packageName);

        var root = new Tree($"[{StyleManager.Content.ToMarkup()}]{packageName}/[/]");

        // Root files
        root.AddNode($"[{StyleManager.Dim.ToMarkup()}]{StyleManager.TreeBranch} package.json[/]");
        root.AddNode($"[{StyleManager.Dim.ToMarkup()}]{StyleManager.TreeEnd} README.md[/]");

        // Modules
        if (template.Modules.Count > 0)
        {
            var modulesNode = root.AddNode($"[{StyleManager.Content.ToMarkup()}]{StyleManager.TreeBranch} Modules[/]");
            var moduleList = template.Modules.ToList();

            for (int i = 0; i < moduleList.Count; i++)
            {
                var isLast = i == moduleList.Count - 1;
                var prefix = isLast ? StyleManager.TreeEnd : StyleManager.TreeBranch;
                var modName = moduleList[i] switch
                {
                    "Data" => $"{asmdefRoot}.Data",
                    "Authoring" => $"{asmdefRoot}.Authoring",
                    "Runtime" => $"{asmdefRoot}.Runtime",
                    "Systems" => $"{asmdefRoot}.Systems",
                    "Editor" => $"{asmdefRoot}.Editor",
                    "Debug" => $"{asmdefRoot}.Debug",
                    "Tests" => $"{asmdefRoot}.Tests",
                    _ => moduleList[i]
                };
                modulesNode.AddNode($"[{StyleManager.Dim.ToMarkup()}]{prefix} {modName}/[/]");
            }
        }

        return root;
    }
}
