using Spectre.Console;
using PackageSmith.Commands;

namespace PackageSmith.UI;

public static class InteractiveMode
{
    public static int Run()
    {
        while (true)
        {
            // Simple, clean menu rendered at the cursor position
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[{StyleManager.Primary.ToMarkup()}]What would you like to do?[/]")
                    .PageSize(10)
                    .HighlightStyle(new Style(foreground: StyleManager.Primary))
                    .AddChoices(new[]
                    {
                        "Create New Package",
                        "View Templates",
                        "Install Package to Project",
                        "Transfer Package",
                        "Configure Settings",
                        "Exit"
                    }));

            switch (choice)
            {
                case "Create New Package":
                    new NewCommand().Execute(null!, new NewCommand.Settings { NoWizard = false });
                    break;

                case "View Templates":
                    new TemplatesCommand().Execute(null!, new TemplatesCommand.Settings { Action = "list" });
                    break;

                case "Install Package to Project":
                    new InstallCommand().Execute(null!, new InstallCommand.Settings());
                    break;

                case "Transfer Package":
                    var pkg = AnsiConsole.Ask<string>($"[{StyleManager.Primary.ToMarkup()}]Enter package name:[/]");
                    new TransferCommand().Execute(null!, new TransferCommand.Settings { PackageName = pkg });
                    break;

                case "Configure Settings":
                    new SettingsCommand().Execute(null!, new SettingsCommand.Settings());
                    break;

                case "Exit":
                    return 0;
            }

            AnsiConsole.WriteLine();

            // "Press any key" style pause instead of prompt, then loop
            // But confirming is safer
            if (!AnsiConsole.Confirm("Return to menu?", defaultValue: true))
            {
                return 0;
            }

            // Just add a divider, DO NOT CLEAR
            LayoutManager.PrintSection("PackageSmith");
        }
    }
}
