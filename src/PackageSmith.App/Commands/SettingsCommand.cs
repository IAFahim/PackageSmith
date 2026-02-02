using PackageSmith.App.Bridges;
using PackageSmith.Data.Config;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PackageSmith.App.Commands;

public sealed class SettingsCommand : Command<SettingsCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        AnsiConsole.MarkupLine("[steelblue]PackageSmith Settings[/]");
        AnsiConsole.WriteLine();

        var bridge = new ConfigBridge();

        if (!bridge.TryLoad(out var config))
        {
            AnsiConsole.MarkupLine("[yellow]No configuration found. Using defaults.[/]");
            config = bridge.GetDefault();
        }

        DisplayConfig(in config);

        return 0;
    }

    private void DisplayConfig(in AppConfig config)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[steelblue]Setting[/]")
            .AddColumn("[steelblue]Value[/]");

        table.AddRow("Company", config.CompanyName);
        table.AddRow("Email", config.AuthorEmail);
        table.AddRow("Unity Version", config.DefaultUnityVersion);
        table.AddRow("Website", string.IsNullOrEmpty(config.Website) ? "[grey]N/A[/]" : config.Website);

        AnsiConsole.Write(table);
    }

    public sealed class Settings : CommandSettings
    {
        [CommandOption("-f|--force")] public bool Force { get; init; }
    }
}