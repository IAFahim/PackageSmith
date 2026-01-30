using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using PackageSmith.Core.Validation;

namespace PackageSmith.Commands;

public sealed class ValidateCommand : Command<ValidateCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[path]")]
        [Description("Path to the package to validate (default: current directory)")]
        public string? Path { get; init; }

        [CommandOption("-v|--verbose")]
        [Description("Show detailed validation output")]
        public bool Verbose { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var path = settings.Path ?? Directory.GetCurrentDirectory();

        if (!Directory.Exists(path))
        {
            AnsiConsole.MarkupLine($"[red]Path not found:[/] {path}");
            return 1;
        }

        var validator = new PackageValidator();
        var result = validator.Validate(path);

        DisplayResults(result, settings.Verbose);

        return result.IsValid ? 0 : 1;
    }

    private static void DisplayResults(in Core.Validation.ValidationResult result, bool verbose)
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[yellow]Check[/]");
        table.AddColumn("[yellow]Status[/]");

        if (verbose)
        {
            table.AddColumn("[yellow]Details[/]");
        }

        foreach (var check in result.Checks)
        {
            var status = check.Passed ? "[green]PASS[/]" : "[red]FAIL[/]";
            var details = verbose ? check.Message : "";

            if (verbose)
            {
                table.AddRow(check.Name, status, details);
            }
            else
            {
                table.AddRow(check.Name, status);
            }
        }

        AnsiConsole.Write(table);

        if (result.IsValid)
        {
            AnsiConsole.MarkupLine($"\n[green]All {result.Checks.Length} checks passed![/]");
        }
        else
        {
            var failed = result.Checks.Count(c => !c.Passed);
            AnsiConsole.MarkupLine($"\n[red]{failed} check(s) failed. Use --verbose for details.[/]");
        }
    }
}
