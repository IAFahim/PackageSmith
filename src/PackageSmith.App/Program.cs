using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PackageSmith.App.Commands;
using PackageSmith.App.UX;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PackageSmith.App;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            AnsiConsole.MarkupLine($"[bold cyan]PackageSmith[/] v{version}");
            return 0;
        }

        if (args.Length == 0)
        {
            ShowBanner();
            return StateMachine.Run();
        }

        if (!args.Any(x => x == "--json") && !args.Any(x => x == "--quiet")) ShowBanner();

        var stopwatch = Stopwatch.StartNew();
        var app = new CommandApp();

        app.Configure(config =>
        {
            config.SetApplicationName("pksmith");

            config.AddCommand<NewCommand>("new")
                .WithDescription("Create a new Unity package");

            config.AddCommand<TemplatesCommand>("templates")
                .WithDescription("List available templates");

            config.AddCommand<SettingsCommand>("settings")
                .WithDescription("Configure global package settings");

            config.AddCommand<CiCommand>("ci")
                .WithDescription("Manage CI/CD workflows");

            config.AddCommand<HarvestCommand>("harvest")
                .WithDescription("Harvest a local package into a template");

            config.SetInterceptor(new CommandInterceptor());
        });

        var result = app.Run(args);
        stopwatch.Stop();

        if (result == 0 && stopwatch.ElapsedMilliseconds > 500)
            AnsiConsole.MarkupLine($"\n[dim]Done in {FormatDuration(stopwatch.Elapsed)}[/]");

        return result;
    }

    private static void ShowBanner()
    {
        if (Console.IsOutputRedirected) return;

        AnsiConsole.MarkupLine("[bold cyan]◆[/] [bold white]PackageSmith[/] [dim]Unity Package Scaffolding[/]");
        AnsiConsole.WriteLine();
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
            return $"{duration.TotalMilliseconds:F0}ms";
        if (duration.TotalMinutes < 1)
            return $"{duration.TotalSeconds:F1}s";
        return $"{duration.TotalMinutes:F1}m";
    }
}

internal class CommandInterceptor : ICommandInterceptor
{
    public void Intercept(CommandContext context, CommandSettings settings, IRemainingArguments args)
    {
        _ = Task.Run(async () => await Task.CompletedTask);
    }
}