using Spectre.Console;
using Spectre.Console.Cli;
using PackageSmith.Commands;
using PackageSmith.UI;
using System.Diagnostics;

namespace PackageSmith;

static class Program
{
    static int Main(string[] args)
    {
        // Global exception handler
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                AnsiConsole.MarkupLine($"\n[{StyleManager.ErrorColor.ToMarkup()}]{StyleManager.IconError} An unexpected error occurred[/]");
                AnsiConsole.MarkupLine($"[{StyleManager.MutedColor.ToMarkup()}]Report issues at: https://github.com/your-repo/pksmith/issues[/]");
                Environment.Exit(1);
            }
        };

        ShowBanner();

        var stopwatch = Stopwatch.StartNew();
        var app = new CommandApp();

        app.Configure(config =>
        {
            config.SetApplicationName("pksmith");

            config.AddCommand<NewCommand>("new")
                .WithDescription("Create a new Unity package from template")
                .WithExample("new", "ecs-modular")
                .WithExample("new", "--interactive")
                .WithExample("new", "ecs-modular --name com.studio.feature");

            config.AddCommand<TemplatesCommand>("templates")
                .WithDescription("Manage and discover package templates")
                .WithExample("templates", "list")
                .WithExample("templates", "info ecs-modular")
                .WithExample("templates", "preview ecs-modular");

            config.AddCommand<InstallCommand>("install")
                .WithDescription("Install a Unity package from local path")
                .WithExample("install", "../MyPackage")
                .WithExample("install", ".");

            config.AddCommand<TransferCommand>("transfer")
                .WithDescription("Transfer packages between Library and Packages folders")
                .WithExample("transfer", "com.company.package")
                .WithExample("transfer", "com.company.package --to-packages")
                .WithExample("transfer", "com.company.package --to-library");

            config.AddCommand<GitCommand>("git")
                .WithDescription("Manage git repositories for packages")
                .WithExample("git", "link com.company.package https://github.com/user/repo.git")
                .WithExample("git", "clone https://github.com/user/com.company.package.git")
                .WithExample("git", "status com.company.package")
                .WithExample("git", "push com.company.package");

            config.AddCommand<ListCommand>("list")
                .WithDescription("List installed packages in Unity project")
                .WithExample("list");

            config.AddCommand<RemoveCommand>("remove")
                .WithDescription("Remove a package from Unity project")
                .WithExample("remove", "com.example.mypackage");

            config.AddCommand<ValidateCommand>("validate")
                .WithDescription("Validate package structure and Unity guidelines")
                .WithExample("validate")
                .WithExample("validate", ".")
                .WithExample("validate", "/path/to/package --verbose");

            config.AddCommand<CiCommand>("ci")
                .WithDescription("Generate CI/CD workflows for package testing")
                .WithExample("ci", "generate")
                .WithExample("ci", "generate --unity-versions 2022.3,2023.2")
                .WithExample("ci", "generate --platforms StandaloneWindows64,Android,WebGL")
                .WithExample("ci", "add-secrets");

            config.AddCommand<SettingsCommand>("settings")
                .WithDescription("Configure global package settings")
                .WithExample("settings")
                .WithExample("settings", "--force");

            config.AddCommand<UpdateCommand>("update")
                .WithDescription("Update pksmith to latest version")
                .WithExample("update");

            // Set up command interceptor for timing
            config.SetInterceptor(new CommandInterceptor());
        });

        var result = app.Run(args);
        stopwatch.Stop();

        if (result == 0 && stopwatch.ElapsedMilliseconds > 500)
        {
            AnsiConsole.MarkupLine($"\n[{StyleManager.MutedColor.ToMarkup()}]{StyleManager.IconInfo} Done in {FormatDuration(stopwatch.Elapsed)}[/]");
        }

        return result;
    }

    private static void ShowBanner()
    {
        if (Console.IsOutputRedirected) return;

        LayoutManager.PrintHeader();
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
        // Check for updates (non-blocking)
        _ = Task.Run(async () =>
        {
            try
            {
                // TODO: Implement version check
                await Task.CompletedTask;
            }
            catch
            {
                // Ignore update check errors
            }
        });
    }
}
