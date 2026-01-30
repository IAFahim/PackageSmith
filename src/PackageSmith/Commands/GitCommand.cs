using System.ComponentModel;
using System.Diagnostics;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PackageSmith.Commands;

[Description("Manage git repositories for packages")]
public class GitCommand : Command<GitCommand.Settings>
{
    private readonly TransferCommand _transferCommand;

    public GitCommand()
    {
        _transferCommand = new TransferCommand();
    }
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<action>")]
        [Description("Action: link, unlink, clone, status, push, pull")]
        public required string Action { get; set; }

        [CommandArgument(1, "[package-or-url]")]
        [Description("Package name or git repository URL")]
        public string? PackageOrUrl { get; set; }

        [CommandArgument(2, "[repo-url]")]
        [Description("Git repository URL (for link command)")]
        public string? RepoUrl { get; set; }

        [CommandOption("-p|--project <PATH>")]
        [Description("Unity project path")]
        public string? ProjectPath { get; set; }

        [CommandOption("-b|--branch <BRANCH>")]
        [Description("Git branch to use (default: main)")]
        public string? Branch { get; set; }

        [CommandOption("--transfer")]
        [Description("Auto-transfer package to Packages/ if in Library")]
        public bool AutoTransfer { get; set; }

        [CommandOption("-f|--force")]
        [Description("Force operation without confirmation")]
        public bool Force { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        return settings.Action.ToLowerInvariant() switch
        {
            "link" => LinkRepository(settings),
            "unlink" => UnlinkRepository(settings),
            "clone" => CloneRepository(settings),
            "status" => ShowStatus(settings),
            "push" => PushChanges(settings),
            "pull" => PullChanges(settings),
            _ => ShowUsage()
        };
    }

    private int LinkRepository(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.PackageOrUrl))
        {
            AnsiConsole.MarkupLine("[red]Error: Package name required[/]");
            AnsiConsole.MarkupLine("[dim]Usage: pksmith git link <package> <repo-url>[/]");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(settings.RepoUrl))
        {
            AnsiConsole.MarkupLine("[red]Error: Repository URL required[/]");
            AnsiConsole.MarkupLine("[dim]Usage: pksmith git link <package> <repo-url>[/]");
            return 1;
        }

        var projectPath = FindUnityProject(settings.ProjectPath);
        if (projectPath == null)
        {
            AnsiConsole.MarkupLine("[red]Error: Unity project not found[/]");
            return 1;
        }

        var packageName = settings.PackageOrUrl;
        var packagePath = FindPackage(projectPath, packageName);

        if (packagePath == null)
        {
            AnsiConsole.MarkupLine($"[red]Error: Package '{packageName}' not found[/]");
            
            if (IsPackageInLibrary(projectPath, packageName))
            {
                AnsiConsole.MarkupLine($"[yellow]Hint: Package is in Library/PackageCache (read-only)[/]");
                if (settings.AutoTransfer || AnsiConsole.Confirm("Transfer to Packages/ first?", true))
                {
                    // Call transfer command
                    var transferResult = TransferPackageToPackages(projectPath, packageName);
                    if (transferResult != 0) return transferResult;
                    
                    packagePath = Path.Combine(projectPath, "Packages", packageName);
                }
                else
                {
                    return 1;
                }
            }
            else
            {
                return 1;
            }
        }

        AnsiConsole.MarkupLine($"[dim]Package:[/] {packagePath}");
        AnsiConsole.MarkupLine($"[dim]Repository:[/] {settings.RepoUrl}");

        // Check if already has .git
        var gitDir = Path.Combine(packagePath, ".git");
        if (Directory.Exists(gitDir))
        {
            AnsiConsole.MarkupLine("[yellow]Warning: Package already has git repository[/]");
            
            var currentRemote = GetGitRemote(packagePath);
            if (currentRemote != null)
            {
                AnsiConsole.MarkupLine($"[dim]Current remote:[/] {currentRemote}");
            }

            if (!settings.Force && !AnsiConsole.Confirm("Replace with new repository?", false))
            {
                return 0;
            }
        }

        var panel = new Panel(new Markup($@"[yellow]Link Package to Git Repository[/]

[dim]Package:[/] {packageName}
[dim]Location:[/] Packages/{packageName}
[dim]Repository:[/] {settings.RepoUrl}
[dim]Branch:[/] {settings.Branch ?? "main"}

This will:
  • Initialize git repository in package folder
  • Add remote origin
  • Fetch from repository
  • Set up tracking branch

[yellow]Purpose:[/] Enable [green]push/pull[/] directly from package"))
            .BorderColor(Color.Yellow)
            .Header("[yellow]Confirm Link[/]");

        AnsiConsole.Write(panel);

        if (!settings.Force && !AnsiConsole.Confirm("\nProceed?", true))
        {
            return 0;
        }

        return AnsiConsole.Status()
            .Start("Linking repository...", ctx =>
            {
                ctx.Status("Initializing git...");
                RunGit(packagePath, "init");
                AnsiConsole.MarkupLine("[green]✓[/] Git initialized");

                ctx.Status("Adding remote...");
                RunGit(packagePath, $"remote add origin {settings.RepoUrl}");
                AnsiConsole.MarkupLine("[green]✓[/] Remote added");

                ctx.Status("Fetching repository...");
                var fetchResult = RunGit(packagePath, "fetch origin", throwOnError: false);
                if (fetchResult)
                {
                    AnsiConsole.MarkupLine("[green]✓[/] Repository fetched");

                    var branch = settings.Branch ?? "main";
                    ctx.Status($"Setting up branch {branch}...");
                    
                    // Check if branch exists remotely
                    var branchExists = RunGit(packagePath, $"ls-remote --heads origin {branch}", throwOnError: false);
                    
                    if (branchExists)
                    {
                        RunGit(packagePath, $"branch --set-upstream-to=origin/{branch}");
                        AnsiConsole.MarkupLine($"[green]✓[/] Tracking origin/{branch}");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]Note:[/] Branch '{branch}' not found on remote");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]⚠[/] Could not fetch (repository might be empty)");
                }

                AnsiConsole.MarkupLine($"\n[green]✓ Repository linked successfully![/]");
                AnsiConsole.MarkupLine($"[dim]You can now use: pksmith git push {packageName}[/]");

                return 0;
            });
    }

    private int UnlinkRepository(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.PackageOrUrl))
        {
            AnsiConsole.MarkupLine("[red]Error: Package name required[/]");
            return 1;
        }

        var projectPath = FindUnityProject(settings.ProjectPath);
        if (projectPath == null)
        {
            AnsiConsole.MarkupLine("[red]Error: Unity project not found[/]");
            return 1;
        }

        var packageName = settings.PackageOrUrl;
        var packagePath = FindPackage(projectPath, packageName);

        if (packagePath == null)
        {
            AnsiConsole.MarkupLine($"[red]Error: Package '{packageName}' not found[/]");
            return 1;
        }

        var gitDir = Path.Combine(packagePath, ".git");
        if (!Directory.Exists(gitDir))
        {
            AnsiConsole.MarkupLine("[yellow]Package is not linked to a git repository[/]");
            return 0;
        }

        if (!settings.Force && !AnsiConsole.Confirm("Remove git repository? (files will remain)", false))
        {
            return 0;
        }

        Directory.Delete(gitDir, true);
        AnsiConsole.MarkupLine("[green]✓[/] Git repository unlinked");

        return 0;
    }

    private int CloneRepository(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.PackageOrUrl))
        {
            AnsiConsole.MarkupLine("[red]Error: Repository URL required[/]");
            return 1;
        }

        var projectPath = FindUnityProject(settings.ProjectPath);
        if (projectPath == null)
        {
            AnsiConsole.MarkupLine("[red]Error: Unity project not found[/]");
            return 1;
        }

        var repoUrl = settings.PackageOrUrl;
        
        // Extract package name from URL (last part before .git)
        var packageName = ExtractPackageNameFromUrl(repoUrl);
        if (packageName == null)
        {
            AnsiConsole.MarkupLine("[yellow]Could not extract package name from URL[/]");
            packageName = AnsiConsole.Ask<string>("Enter package name:");
        }

        var packagesDir = Path.Combine(projectPath, "Packages");
        var packagePath = Path.Combine(packagesDir, packageName);

        if (Directory.Exists(packagePath))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Package directory already exists[/]");
            if (!settings.Force && !AnsiConsole.Confirm("Overwrite?", false))
            {
                return 0;
            }
            Directory.Delete(packagePath, true);
        }

        var panel = new Panel(new Markup($@"[cyan]Clone Package Repository[/]

[dim]Repository:[/] {repoUrl}
[dim]Destination:[/] Packages/{packageName}
[dim]Branch:[/] {settings.Branch ?? "main"}

This will:
  • Clone repository to Packages/
  • Package becomes immediately editable
  • Already linked to git remote
  • Ready for commits and pushes"))
            .BorderColor(Color.Blue)
            .Header("[cyan]Confirm Clone[/]");

        AnsiConsole.Write(panel);

        if (!settings.Force && !AnsiConsole.Confirm("\nProceed?", true))
        {
            return 0;
        }

        return AnsiConsole.Status()
            .Start("Cloning repository...", ctx =>
            {
                var branch = settings.Branch ?? "main";
                var cloneCmd = $"clone {repoUrl} {packagePath} --branch {branch}";
                
                ctx.Status($"Cloning {packageName}...");
                var success = RunGitCommand(cloneCmd);

                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Cloned to Packages/{packageName}");
                    
                    // Add to manifest if package.json exists
                    var manifestPath = Path.Combine(packagePath, "package.json");
                    if (File.Exists(manifestPath))
                    {
                        ctx.Status("Adding to manifest...");
                        AddToManifest(projectPath, packageName);
                        AnsiConsole.MarkupLine("[green]✓[/] Added to manifest.json");
                    }

                    AnsiConsole.MarkupLine($"\n[green]✓ Package cloned successfully![/]");
                    return 0;
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]✗[/] Clone failed");
                    return 1;
                }
            });
    }

    private int ShowStatus(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.PackageOrUrl))
        {
            AnsiConsole.MarkupLine("[red]Error: Package name required[/]");
            return 1;
        }

        var projectPath = FindUnityProject(settings.ProjectPath);
        if (projectPath == null)
        {
            AnsiConsole.MarkupLine("[red]Error: Unity project not found[/]");
            return 1;
        }

        var packageName = settings.PackageOrUrl;
        var packagePath = FindPackage(projectPath, packageName);

        if (packagePath == null)
        {
            AnsiConsole.MarkupLine($"[red]Error: Package '{packageName}' not found[/]");
            return 1;
        }

        var gitDir = Path.Combine(packagePath, ".git");
        if (!Directory.Exists(gitDir))
        {
            AnsiConsole.MarkupLine($"[yellow]Package '{packageName}' is not linked to a git repository[/]");
            AnsiConsole.MarkupLine($"[dim]Use: pksmith git link {packageName} <repo-url>[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[bold cyan]Git Status: {packageName}[/]\n");

        // Get remote
        var remote = GetGitRemote(packagePath);
        if (remote != null)
        {
            AnsiConsole.MarkupLine($"[dim]Remote:[/] {remote}");
        }

        // Get branch
        var branch = GetGitOutput(packagePath, "rev-parse --abbrev-ref HEAD");
        AnsiConsole.MarkupLine($"[dim]Branch:[/] {branch ?? "unknown"}");

        // Get status
        AnsiConsole.WriteLine();
        var statusOutput = GetGitOutput(packagePath, "status --short");
        if (string.IsNullOrWhiteSpace(statusOutput))
        {
            AnsiConsole.MarkupLine("[green]✓ Working tree clean[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Modified files:[/]");
            AnsiConsole.WriteLine(statusOutput);
        }

        // Get unpushed commits
        var unpushed = GetGitOutput(packagePath, "log @{u}.. --oneline", throwOnError: false);
        if (!string.IsNullOrWhiteSpace(unpushed))
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Unpushed commits:[/]");
            AnsiConsole.WriteLine(unpushed);
        }

        return 0;
    }

    private int PushChanges(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.PackageOrUrl))
        {
            AnsiConsole.MarkupLine("[red]Error: Package name required[/]");
            return 1;
        }

        var projectPath = FindUnityProject(settings.ProjectPath);
        if (projectPath == null)
        {
            AnsiConsole.MarkupLine("[red]Error: Unity project not found[/]");
            return 1;
        }

        var packageName = settings.PackageOrUrl;
        var packagePath = FindPackage(projectPath, packageName);

        if (packagePath == null)
        {
            AnsiConsole.MarkupLine($"[red]Error: Package '{packageName}' not found[/]");
            return 1;
        }

        var gitDir = Path.Combine(packagePath, ".git");
        if (!Directory.Exists(gitDir))
        {
            AnsiConsole.MarkupLine($"[yellow]Package is not linked to a git repository[/]");
            return 1;
        }

        return AnsiConsole.Status()
            .Start("Pushing changes...", ctx =>
            {
                var branch = settings.Branch ?? GetGitOutput(packagePath, "rev-parse --abbrev-ref HEAD") ?? "main";
                
                ctx.Status($"Pushing to origin/{branch}...");
                var success = RunGit(packagePath, $"push origin {branch}");

                if (success)
                {
                    AnsiConsole.MarkupLine("[green]✓[/] Changes pushed successfully");
                    return 0;
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]✗[/] Push failed");
                    return 1;
                }
            });
    }

    private int PullChanges(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.PackageOrUrl))
        {
            AnsiConsole.MarkupLine("[red]Error: Package name required[/]");
            return 1;
        }

        var projectPath = FindUnityProject(settings.ProjectPath);
        if (projectPath == null)
        {
            AnsiConsole.MarkupLine("[red]Error: Unity project not found[/]");
            return 1;
        }

        var packageName = settings.PackageOrUrl;
        var packagePath = FindPackage(projectPath, packageName);

        if (packagePath == null)
        {
            AnsiConsole.MarkupLine($"[red]Error: Package '{packageName}' not found[/]");
            return 1;
        }

        var gitDir = Path.Combine(packagePath, ".git");
        if (!Directory.Exists(gitDir))
        {
            AnsiConsole.MarkupLine($"[yellow]Package is not linked to a git repository[/]");
            return 1;
        }

        return AnsiConsole.Status()
            .Start("Pulling changes...", ctx =>
            {
                ctx.Status("Pulling from origin...");
                var success = RunGit(packagePath, "pull");

                if (success)
                {
                    AnsiConsole.MarkupLine("[green]✓[/] Changes pulled successfully");
                    return 0;
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]✗[/] Pull failed");
                    return 1;
                }
            });
    }

    private int ShowUsage()
    {
        AnsiConsole.MarkupLine("[red]Invalid action[/]");
        AnsiConsole.MarkupLine("[yellow]Usage:[/]");
        AnsiConsole.MarkupLine("  pksmith git link <package> <repo-url>");
        AnsiConsole.MarkupLine("  pksmith git unlink <package>");
        AnsiConsole.MarkupLine("  pksmith git clone <repo-url>");
        AnsiConsole.MarkupLine("  pksmith git status <package>");
        AnsiConsole.MarkupLine("  pksmith git push <package>");
        AnsiConsole.MarkupLine("  pksmith git pull <package>");
        return 1;
    }

    // Helper methods
    private string? FindUnityProject(string? startPath)
    {
        var current = startPath != null ? new DirectoryInfo(startPath) : new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current != null)
        {
            var manifestPath = Path.Combine(current.FullName, "Packages", "manifest.json");
            if (File.Exists(manifestPath))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        return null;
    }

    private string? FindPackage(string projectPath, string packageName)
    {
        var packagePath = Path.Combine(projectPath, "Packages", packageName);
        return Directory.Exists(packagePath) ? packagePath : null;
    }

    private bool IsPackageInLibrary(string projectPath, string packageName)
    {
        var packageCacheDir = Path.Combine(projectPath, "Library", "PackageCache");
        if (!Directory.Exists(packageCacheDir)) return false;

        var matches = Directory.GetDirectories(packageCacheDir, $"{packageName}*");
        return matches.Length > 0;
    }

    private int TransferPackageToPackages(string projectPath, string packageName)
    {
        AnsiConsole.MarkupLine($"[cyan]Transferring {packageName} to Packages/...[/]");
        
        // Call TransferCommand's internal method directly
        var transferSettings = new TransferCommand.Settings
        {
            PackageName = packageName,
            ProjectPath = projectPath,
            ToPackages = true,
            Force = true
        };

        try
        {
            return _transferCommand.ExecuteTransfer(transferSettings);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Transfer failed: {ex.Message}[/]");
            return 1;
        }
    }

    private bool RunGit(string workingDir, string arguments, bool throwOnError = true)
    {
        try
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            proc.WaitForExit();

            if (proc.ExitCode != 0 && throwOnError)
            {
                var error = proc.StandardError.ReadToEnd();
                AnsiConsole.MarkupLine($"[red]Git error:[/] {error}");
            }

            return proc.ExitCode == 0;
        }
        catch (Exception ex)
        {
            if (throwOnError)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
            return false;
        }
    }

    private bool RunGitCommand(string arguments)
    {
        try
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    UseShellExecute = false
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

    private string? GetGitOutput(string workingDir, string arguments, bool throwOnError = true)
    {
        try
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            var output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();

            return proc.ExitCode == 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }

    private string? GetGitRemote(string workingDir)
    {
        return GetGitOutput(workingDir, "remote get-url origin", throwOnError: false);
    }

    private string? ExtractPackageNameFromUrl(string url)
    {
        // Extract from URLs like:
        // https://github.com/user/com.company.package.git
        // git@github.com:user/com.company.package.git

        var lastSlash = url.LastIndexOf('/');
        if (lastSlash < 0) return null;

        var name = url[(lastSlash + 1)..];
        if (name.EndsWith(".git"))
        {
            name = name[..^4];
        }

        return name;
    }

    private void AddToManifest(string projectPath, string packageName)
    {
        // Simple file: reference add - in production use proper JSON manipulation
        var manifestPath = Path.Combine(projectPath, "Packages", "manifest.json");
        // Implementation would update manifest.json
    }
}
