using System.ComponentModel;
using System.Diagnostics;
using Spectre.Console;
using Spectre.Console.Cli;
using PackageSmith.Core.Configuration;

namespace PackageSmith.Commands;

public sealed class SettingsCommand : Command<SettingsCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-f|--force")]
        [Description("Force re-configuration, overwriting existing settings")]
        public bool Force { get; init; }
    }

    private readonly IConfigService _configService;

    public SettingsCommand()
    {
        _configService = new ConfigService();
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (_configService.ConfigExists() && !settings.Force)
        {
            _configService.TryLoadConfig(out var existing);
            AnsiConsole.MarkupLine($"[yellow]Configuration already exists:[/]");
            AnsiConsole.MarkupLine(existing.ToDisplayString());

            if (!AnsiConsole.Confirm("\nDo you want to reconfigure?"))
            {
                return 0;
            }
        }

        return RunWizard();
    }

    private int RunWizard()
    {
        AnsiConsole.Clear();
        PrintHeader();

        var config = new PackageSmithConfig
        {
            CompanyName = PromptCompanyName(),
            AuthorEmail = PromptEmail(),
            Website = PromptWebsite() ?? string.Empty,
            DefaultUnityVersion = PromptUnityVersion()
        };

        if (!_configService.TrySaveConfig(config))
        {
            AnsiConsole.MarkupLine("[red]Failed to save configuration[/]");
            return 1;
        }

        AnsiConsole.MarkupLine("\n[green][[Configuration Saved]][/]");
        AnsiConsole.MarkupLine($"Location: [link]{_configService.GetConfigPath()}[/]");
        
        // Reload config to display with correct timestamp
        if (_configService.TryLoadConfig(out var savedConfig))
        {
            AnsiConsole.MarkupLine(savedConfig.ToDisplayString());
        }
        else
        {
            AnsiConsole.MarkupLine(config.ToDisplayString());
        }

        return 0;
    }

    private static void PrintHeader()
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddRow(new Text("PackageSmith", new Style(Color.Purple, decoration: Decoration.Bold)));
        grid.AddRow(new Text("First-Time Setup", new Style(Color.White, decoration: Decoration.Italic)));
        grid.AddRow(Text.Empty);

        AnsiConsole.Write(grid);
        AnsiConsole.MarkupLine("This wizard will configure your default settings for package creation.");
        AnsiConsole.MarkupLine("These values will be used as defaults and can be overridden per package.\n");
    }

    private static string PromptCompanyName()
    {
        // Try to get from git config
        var gitName = TryGetGitConfig("user.name");
        
        return AnsiConsole.Prompt(
            new TextPrompt<string>("[cyan]Company/Author name[/]:")
                .PromptStyle("white")
                .DefaultValue(gitName ?? string.Empty)
                .AllowEmpty()
                .ShowDefaultValue(gitName != null)
                .Validate(name =>
                {
                    if (string.IsNullOrWhiteSpace(name))
                        return ValidationResult.Error("Company name cannot be empty");
                    return ValidationResult.Success();
                })
        );
    }

    private static string PromptEmail()
    {
        // Try to get from git config
        var gitEmail = TryGetGitConfig("user.email");
        
        return AnsiConsole.Prompt(
            new TextPrompt<string>("[cyan]Author email[/]:")
                .PromptStyle("white")
                .DefaultValue(gitEmail ?? string.Empty)
                .AllowEmpty()
                .ShowDefaultValue(gitEmail != null)
                .Validate(email =>
                {
                    if (string.IsNullOrWhiteSpace(email))
                        return ValidationResult.Error("Email cannot be empty");
                    if (!email.Contains('@'))
                        return ValidationResult.Error("Invalid email format");
                    return ValidationResult.Success();
                })
        );
    }

    private static string? PromptWebsite()
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string?>("[cyan dim](Optional)[/] Website URL:")
                .AllowEmpty()
                .PromptStyle("white")
        );
    }

    private static string PromptUnityVersion()
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>("[cyan]Default Unity version[/]:")
                .DefaultValue("2022.3")
                .PromptStyle("white")
                .Validate(version =>
                {
                    if (string.IsNullOrWhiteSpace(version))
                        return ValidationResult.Error("Unity version cannot be empty");
                    return ValidationResult.Success();
                })
        );
    }

    private static string? TryGetGitConfig(string key)
    {
        try
        {
            var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"config --global {key}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });

            if (proc == null) return null;
            
            proc.WaitForExit();
            
            if (proc.ExitCode == 0)
            {
                var output = proc.StandardOutput.ReadToEnd().Trim();
                return string.IsNullOrWhiteSpace(output) ? null : output;
            }
        }
        catch
        {
            // Git not installed or config not set
        }

        return null;
    }
}
