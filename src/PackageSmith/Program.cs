using Spectre.Console;
using Spectre.Console.Cli;
using PackageSmith.Commands;

namespace PackageSmith;

static class Program
{
    static int Main(string[] args)
    {
        ShowBanner();
        
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

            config.AddCommand<SettingsCommand>("settings")
                .WithDescription("Configure global package settings")
                .WithExample("settings")
                .WithExample("settings", "--force");

            config.AddCommand<UpdateCommand>("update")
                .WithDescription("Update pksmith to latest version")
                .WithExample("update");
        });

        return app.Run(args);
    }

    private static void ShowBanner()
    {
        if (Console.IsOutputRedirected) return;

        AnsiConsole.Write(
            new FigletText("PackageSmith")
                .Color(Color.Cyan1)
        );
        
        AnsiConsole.MarkupLine("[dim]Build Unity packages the right way, every time.[/]");
        AnsiConsole.MarkupLine("[dim]Version 2.0.0[/]\n");
    }
}
