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
        });

        return app.Run(args);
    }
}
