using Spectre.Console;

namespace PackageSmith.UI;

public static class PromptManager
{
    public static string PromptPackageName(string? defaultValue = null)
    {
        LayoutManager.PrintSection("Package Name");

        AnsiConsole.MarkupLine($"[{StyleManager.InfoColor.ToMarkup()}]Enter package name in reverse domain notation:[/]");
        AnsiConsole.MarkupLine($"[{StyleManager.MutedColor.ToMarkup()}]Example: com.company.feature[/]");
        AnsiConsole.WriteLine();

        while (true)
        {
            var prompt = new TextPrompt<string>($"[{StyleManager.CommandColor.ToMarkup()}]Package name:[/]")
                .PromptStyle(StyleManager.Command);

            if (!string.IsNullOrEmpty(defaultValue))
            {
                prompt = prompt.DefaultValue(defaultValue)
                    .ShowDefaultValue(true);
            }

            var result = AnsiConsole.Prompt(prompt);

            // Validate
            if (string.IsNullOrWhiteSpace(result))
            {
                AnsiConsole.MarkupLine($"[{StyleManager.ErrorColor.ToMarkup()}]{StyleManager.SymError} Package name cannot be empty[/]");
                continue;
            }

            var parts = result.Split('.');
            if (parts.Length < 2)
            {
                AnsiConsole.MarkupLine($"[{StyleManager.ErrorColor.ToMarkup()}]{StyleManager.SymError} Must have at least 2 parts separated by dots (e.g., com.company)[/]");
                continue;
            }

            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part))
                {
                    AnsiConsole.MarkupLine($"[{StyleManager.ErrorColor.ToMarkup()}]{StyleManager.SymError} Each part must be non-empty[/]");
                    continue;
                }

                if (!char.IsLetter(part[0]))
                {
                    AnsiConsole.MarkupLine($"[{StyleManager.ErrorColor.ToMarkup()}]{StyleManager.SymError} Each part must start with a letter[/]");
                    continue;
                }
            }

            // Show validation feedback
            AnsiConsole.MarkupLine($"[{StyleManager.SuccessColor.ToMarkup()}]{StyleManager.SymSuccess} Valid package name[/]");
            AnsiConsole.WriteLine();

            return result;
        }
    }

    public static string PromptDisplayName(string packageName, string? defaultValue = null)
    {
        LayoutManager.PrintSection("Display Name");

        var defaultName = defaultValue ?? ExtractDisplayName(packageName);

        AnsiConsole.MarkupLine($"[{StyleManager.InfoColor.ToMarkup()}]Enter a friendly display name:[/]");
        AnsiConsole.MarkupLine($"[{StyleManager.MutedColor.ToMarkup()}]This will be shown in the Unity Package Manager[/]");
        AnsiConsole.WriteLine();

        var result = AnsiConsole.Prompt(
            new TextPrompt<string>($"[{StyleManager.CommandColor.ToMarkup()}]Display name:[/]")
                .DefaultValue(defaultName)
                .ShowDefaultValue(true)
                .PromptStyle(StyleManager.Command)
        );

        AnsiConsole.MarkupLine($"[{StyleManager.SuccessColor.ToMarkup()}]{StyleManager.SymSuccess} Display name set[/]");
        AnsiConsole.WriteLine();

        return result;

        static string ExtractDisplayName(string packageName)
        {
            var parts = packageName.Split('.');
            var last = parts[^1];
            return char.ToUpper(last[0]) + (last.Length > 1 ? last.Substring(1) : string.Empty);
        }
    }

    public static string PromptDescription()
    {
        LayoutManager.PrintSection("Description");

        AnsiConsole.MarkupLine($"[{StyleManager.InfoColor.ToMarkup()}]Enter a description for your package:[/]");
        AnsiConsole.MarkupLine($"[{StyleManager.MutedColor.ToMarkup()}](optional, press Enter to skip)[/]");
        AnsiConsole.WriteLine();

        var result = AnsiConsole.Prompt(
            new TextPrompt<string>($"[{StyleManager.CommandColor.ToMarkup()}]Description:[/]")
                .AllowEmpty()
                .DefaultValue("")
                .ShowDefaultValue(false)
                .PromptStyle(StyleManager.Command)
        );

        AnsiConsole.WriteLine();

        return result;
    }

    public static string PromptChoice(string title, Dictionary<string, (string Label, string? Description)> choices, string? defaultValue = null)
    {
        LayoutManager.PrintSection(title);

        AnsiConsole.MarkupLine($"[{StyleManager.InfoColor.ToMarkup()}]Select an option:[/]");
        AnsiConsole.MarkupLine($"[{StyleManager.MutedColor.ToMarkup()}](Use arrows to navigate)[/]");
        AnsiConsole.WriteLine();

        var selector = new SelectionPrompt<string>()
            .Title($"[{StyleManager.CommandColor.ToMarkup()}]Choose:[/]")
            .HighlightStyle(StyleManager.Accent)
            .PageSize(10)
            .UseConverter(choice => choices.TryGetValue(choice, out var info) ? info.Label : choice);

        selector = selector.MoreChoicesText($"[{StyleManager.MutedColor.ToMarkup()}](Move up and down to reveal more options)[/]");
        selector = selector.AddChoices(choices.Keys);

        var result = AnsiConsole.Prompt(selector);

        AnsiConsole.MarkupLine($"[{StyleManager.SuccessColor.ToMarkup()}]{StyleManager.SymSuccess} Selected: {choices[result].Label}[/]");
        AnsiConsole.WriteLine();

        return result;
    }

    public static List<T> PromptMultipleChoices<T>(string title, Dictionary<T, (string Label, string? Description)> choices) where T : notnull
    {
        LayoutManager.PrintSection(title);

        AnsiConsole.MarkupLine($"[{StyleManager.InfoColor.ToMarkup()}]Select options:[/]");
        AnsiConsole.MarkupLine($"[{StyleManager.MutedColor.ToMarkup()}](Press Space to select, Enter to confirm)[/]");
        AnsiConsole.WriteLine();

        var selector = new MultiSelectionPrompt<T>()
            .Title($"[{StyleManager.CommandColor.ToMarkup()}]Choose:[/]")
            .HighlightStyle(StyleManager.Accent)
            .PageSize(10)
            .UseConverter(choice => choices.TryGetValue(choice, out var info) ? info.Label : choice.ToString()!)
            .NotRequired()
            .MoreChoicesText($"[{StyleManager.MutedColor.ToMarkup()}](Move up and down to reveal more options)[/]")
            .InstructionsText($"[{StyleManager.MutedColor.ToMarkup()}](Press [{StyleManager.InfoColor.ToMarkup()}]<space>[/] to toggle, [{StyleManager.InfoColor.ToMarkup()}]<enter>[/] to confirm)[/]")
            .AddChoices(choices.Keys);

        var selectionResult = AnsiConsole.Prompt(selector);
        var count = selectionResult.Count();

        AnsiConsole.MarkupLine($"[{StyleManager.SuccessColor.ToMarkup()}]{StyleManager.SymSuccess} Selected {count} option(s)[/]");
        AnsiConsole.WriteLine();

        return selectionResult.ToList();
    }

    public static bool PromptConfirmation(string message, bool defaultValue = true)
    {
        AnsiConsole.WriteLine();

        var result = AnsiConsole.Confirm(
            $"[{StyleManager.WarningColor.ToMarkup()}]{StyleManager.SymWarning} {message}[/]",
            defaultValue
        );

        AnsiConsole.MarkupLine(result
            ? $"[{StyleManager.SuccessColor.ToMarkup()}]{StyleManager.SymSuccess} Confirmed[/]"
            : $"[{StyleManager.MutedColor.ToMarkup()}]{StyleManager.SymInfo} Cancelled[/]");
        AnsiConsole.WriteLine();

        return result;
    }
}
