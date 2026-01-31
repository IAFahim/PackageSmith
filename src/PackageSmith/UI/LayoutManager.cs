using Spectre.Console;

namespace PackageSmith.UI;

public static class LayoutManager
{
    public static void PrintHeader(string? context = null)
    {
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

    public static void PrintKeyboardHint()
    {
        AnsiConsole.MarkupLine($"\n[{StyleManager.Tertiary.ToMarkup()}]{StyleManager.SymInfo} Up/Down Navigate • Enter Select • Esc Cancel[/]\n");
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
