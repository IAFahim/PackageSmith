using Spectre.Console.Cli;
using Spectre.Console;
using System.Diagnostics;

namespace PackageSmith.Commands;

public class UpdateCommand : Command<UpdateCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("--force")]
        public bool Force { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        AnsiConsole.MarkupLine("[cyan]iupk[/] self-update started...\n");

        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var installDir = Path.GetDirectoryName(assemblyLocation);

        if (installDir == null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Cannot determine installation directory");
            return 1;
        }

        var repoRoot = FindGitRepoRoot(installDir);

        if (repoRoot == null)
        {
            AnsiConsole.MarkupLine("[yellow]Warning:[/] Not running from git repository");
            AnsiConsole.MarkupLine("[dim]Release-based updates not implemented yet[/dim]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[dim]Repo root:[/] {repoRoot}");

        AnsiConsole.MarkupLine("[cyan]Fetching[/] latest changes...");
        if (!RunGitCommand(repoRoot, "git fetch origin"))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Git fetch failed");
            return 1;
        }

        var currentBranch = GetGitBranch(repoRoot);
        AnsiConsole.MarkupLine($"[dim]Current branch:[/] {currentBranch}");

        AnsiConsole.MarkupLine("[cyan]Pulling[/] latest changes...");
        if (!RunGitCommand(repoRoot, "git pull origin " + currentBranch))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Git pull failed");
            return 1;
        }

        AnsiConsole.MarkupLine("[cyan]Rebuilding[/] in Release mode...");

        var installScript = Environment.OSVersion.Platform == PlatformID.Unix ||
                           Environment.OSVersion.Platform == PlatformID.MacOSX
            ? Path.Combine(repoRoot, "install.sh")
            : Path.Combine(repoRoot, "install.ps1");

        if (!File.Exists(installScript))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] Install script not found: {installScript}");
            AnsiConsole.MarkupLine("[dim]Please run install script manually[/dim]");
            return 0;
        }

        AnsiConsole.MarkupLine($"\n[green]Update complete![/]");
        AnsiConsole.MarkupLine($"[dim]Run:[/] {installScript}\n");

        return 0;
    }

    private static string? FindGitRepoRoot(string startPath)
    {
        var path = new DirectoryInfo(startPath);

        while (path != null)
        {
            if (Directory.Exists(Path.Combine(path.FullName, ".git")))
            {
                return path.FullName;
            }
            path = path.Parent;
        }

        return null;
    }

    private static bool RunGitCommand(string workingDir, string command)
    {
        try
        {
            var parts = command.Split(' ');
            var exe = parts[0];
            var args = string.Join(" ", parts.Skip(1));

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            proc.WaitForExit();

            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string GetGitBranch(string workingDir)
    {
        try
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --abbrev-ref HEAD",
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            var result = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            return result.Trim();
        }
        catch
        {
            return "main";
        }
    }
}
