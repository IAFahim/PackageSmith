using Spectre.Console;
using Spectre.Console.Cli;
using PackageSmith.Commands;

namespace PackageSmith;

static class Program
{
    static int Main(string[] args)
    {
        var app = new CommandApp();

        app.Configure(config =>
        {
            config.AddCommand<CreateCommand>("create")
                .WithDescription("Create a new Unity package from template")
                .WithExample("create", "com.company.feature")
                .WithExample("create", "com.company.netcode --modules Runtime,Editor");

            config.AddCommand<SettingsCommand>("settings")
                .WithDescription("Configure global package settings")
                .WithExample("settings")
                .WithExample("settings", "--force");

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

            config.AddCommand<UpdateCommand>("update")
                .WithDescription("Update iupk to latest version")
                .WithExample("update");

            config.AddCommand<ValidateCommand>("validate")
                .WithDescription("Validate package structure and Unity guidelines")
                .WithExample("validate")
                .WithExample("validate", ".")
                .WithExample("validate", "/path/to/package --verbose");
        });

        return app.Run(args);
    }
}
