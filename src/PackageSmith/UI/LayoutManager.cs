using Spectre.Console;

namespace PackageSmith.UI;

public static class LayoutManager
{
    private static readonly string _logo = """
        ____    __  ___
    /  _/___/ /_/ /  |_  ____
    / // __  /_  _  __/ __/
  _/ // /_/ / / / / /_/
 /___/\____/_/ /_/\__/  v{0}
""";

    public static void PrintHeader(string? context = null)
    {
        var version = typeof(LayoutManager).Assembly.GetName().Version?.ToString(3) ?? "1.0";
        var logo = string.Format(_logo, version);

        var panel = new Panel(logo)
            .Border(BoxBorder.Rounded)
            .BorderStyle(StyleManager.Command)
            .Header("PackageSmith")
            .HeaderAlignment(Justify.Center)
            .Padding(1, 1, 1, 1);

        if (!string.IsNullOrEmpty(context))
        {
            panel = new Panel(new Markup(logo))
                .Border(BoxBorder.Rounded)
                .BorderStyle(StyleManager.Command)
                .Header("PackageSmith")
                .HeaderAlignment(Justify.Center)
                .Padding(1, 1, 1, 1);
        }

        AnsiConsole.Write(panel);

        if (!string.IsNullOrEmpty(context))
        {
            AnsiConsole.MarkupLine($"[{StyleManager.InfoColor.ToMarkup()}]{context}[/]");
        }
        AnsiConsole.WriteLine();
    }

    public static void PrintSection(string title)
    {
        AnsiConsole.WriteLine();
        var rule = new Rule($"[{StyleManager.AccentColor.ToMarkup()}]{title}[/]")
            .LeftJustified()
            .RuleStyle(StyleManager.Accent);
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
    }

    public static void PrintFooter(string? hint = null)
    {
        AnsiConsole.WriteLine();
        var footer = hint ?? "Use --help for more information";
        var rule = new Rule($"[{StyleManager.MutedColor.ToMarkup()}]{footer}[/]")
            .Justify(Justify.Center)
            .RuleStyle(StyleManager.Muted);
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
    }

    public static void PrintKeyValue(string key, string value, string? icon = null)
    {
        var iconText = string.IsNullOrEmpty(icon) ? "" : $"{icon} ";
        AnsiConsole.MarkupLine($"[{StyleManager.MutedColor.ToMarkup()}]{iconText}{key}:[/] [{StyleManager.InfoColor.ToMarkup()}]{value}[/]");
    }

    public static void PrintMetadataTable(Dictionary<string, string> metadata)
    {
        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderColor(StyleManager.MutedColor)
            .HideHeaders()
            .AddColumn(new TableColumn("") { Width = 20, NoWrap = true })
            .AddColumn(new TableColumn(""));

        foreach (var (key, value) in metadata)
        {
            table.AddRow($"[{StyleManager.MutedColor.ToMarkup()}]{key}[/]", $"[{StyleManager.InfoColor.ToMarkup()}]{value}[/]");
        }

        AnsiConsole.Write(table);
    }
}
