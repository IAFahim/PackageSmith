using System.ComponentModel;
using System.Text;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PackageSmith.Commands;

[Description("Generate CI/CD workflows for package testing")]
public class CiCommand : Command<CiCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[action]")]
        [Description("Action: generate, add-secrets")]
        public string Action { get; set; } = "generate";

        [CommandOption("--package <PATH>")]
        [Description("Package path (default: current directory)")]
        public string? PackagePath { get; set; }

        [CommandOption("--unity-versions <VERSIONS>")]
        [Description("Unity versions to test (comma-separated, e.g., 2022.3,2023.2)")]
        public string? UnityVersions { get; set; }

        [CommandOption("--platforms <PLATFORMS>")]
        [Description("Platforms to build (comma-separated: StandaloneWindows64,StandaloneOSX,StandaloneLinux64,Android,iOS,WebGL)")]
        public string? Platforms { get; set; }

        [CommandOption("--output <PATH>")]
        [Description("Output directory for workflows (default: .github/workflows)")]
        public string? OutputPath { get; set; }

        [CommandOption("--simple")]
        [Description("Generate simple workflow (test only, no builds)")]
        public bool Simple { get; set; }

        [CommandOption("-f|--force")]
        [Description("Overwrite existing workflows")]
        public bool Force { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        return settings.Action.ToLowerInvariant() switch
        {
            "generate" => GenerateWorkflows(settings),
            "add-secrets" => ShowSecretsInfo(),
            _ => ShowUsage()
        };
    }

    private int GenerateWorkflows(Settings settings)
    {
        var packagePath = settings.PackagePath ?? Directory.GetCurrentDirectory();
        
        if (!File.Exists(Path.Combine(packagePath, "package.json")))
        {
            AnsiConsole.MarkupLine("[red]Error: package.json not found[/]");
            AnsiConsole.MarkupLine("[dim]Run this command from package root or use --package[/]");
            return 1;
        }

        var packageJson = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(Path.Combine(packagePath, "package.json"))
        );
        var packageName = packageJson.RootElement.GetProperty("name").GetString() ?? "package";

        var unityVersions = ParseUnityVersions(settings.UnityVersions);
        var platforms = ParsePlatforms(settings.Platforms);
        var outputPath = settings.OutputPath ?? Path.Combine(packagePath, ".github", "workflows");

        AnsiConsole.MarkupLine($"[cyan]Package:[/] {packageName}");
        AnsiConsole.MarkupLine($"[cyan]Unity Versions:[/] {string.Join(", ", unityVersions)}");
        AnsiConsole.MarkupLine($"[cyan]Platforms:[/] {string.Join(", ", platforms)}");
        AnsiConsole.MarkupLine($"[cyan]Output:[/] {outputPath}");
        AnsiConsole.WriteLine();

        if (Directory.Exists(outputPath) && !settings.Force)
        {
            var files = Directory.GetFiles(outputPath, "*.yml");
            if (files.Length > 0)
            {
                AnsiConsole.MarkupLine("[yellow]Warning: Workflow directory already exists with files[/]");
                if (!AnsiConsole.Confirm("Overwrite?", false))
                {
                    return 0;
                }
            }
        }

        Directory.CreateDirectory(outputPath);

        return AnsiConsole.Status()
            .Start("Generating workflows...", ctx =>
            {
                // Generate test workflow
                ctx.Status("Generating test workflow...");
                var testWorkflow = GenerateTestWorkflow(packageName, unityVersions);
                File.WriteAllText(Path.Combine(outputPath, "test.yml"), testWorkflow);
                AnsiConsole.MarkupLine("[green]✓[/] test.yml");

                if (!settings.Simple)
                {
                    // Generate build workflow
                    ctx.Status("Generating build workflow...");
                    var buildWorkflow = GenerateBuildWorkflow(packageName, unityVersions, platforms);
                    File.WriteAllText(Path.Combine(outputPath, "build.yml"), buildWorkflow);
                    AnsiConsole.MarkupLine("[green]✓[/] build.yml");

                    // Generate activation workflow
                    ctx.Status("Generating activation workflow...");
                    var activationWorkflow = GenerateActivationWorkflow();
                    File.WriteAllText(Path.Combine(outputPath, "activation.yml"), activationWorkflow);
                    AnsiConsole.MarkupLine("[green]✓[/] activation.yml");
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[green]✓ Workflows generated successfully![/]");
                AnsiConsole.WriteLine();
                
                ShowNextSteps(settings.Simple);

                return 0;
            });
    }

    private string GenerateTestWorkflow(string packageName, List<string> unityVersions)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("name: Test Package");
        sb.AppendLine();
        sb.AppendLine("on:");
        sb.AppendLine("  push:");
        sb.AppendLine("    branches: [ main, develop ]");
        sb.AppendLine("  pull_request:");
        sb.AppendLine("    branches: [ main, develop ]");
        sb.AppendLine("  workflow_dispatch:");
        sb.AppendLine();
        sb.AppendLine("jobs:");
        sb.AppendLine("  test:");
        sb.AppendLine("    name: Test Unity ${{ matrix.unityVersion }}");
        sb.AppendLine("    runs-on: ubuntu-latest");
        sb.AppendLine("    strategy:");
        sb.AppendLine("      fail-fast: false");
        sb.AppendLine("      matrix:");
        sb.AppendLine($"        unityVersion: [{string.Join(", ", unityVersions.Select(v => $"'{v}'"))}]");
        sb.AppendLine();
        sb.AppendLine("    steps:");
        sb.AppendLine("      # Checkout package");
        sb.AppendLine("      - name: Checkout package");
        sb.AppendLine("        uses: actions/checkout@v4");
        sb.AppendLine("        with:");
        sb.AppendLine("          path: package");
        sb.AppendLine();
        sb.AppendLine("      # Create test Unity project");
        sb.AppendLine("      - name: Create test project");
        sb.AppendLine("        run: |");
        sb.AppendLine("          mkdir -p TestProject/Assets");
        sb.AppendLine("          mkdir -p TestProject/Packages");
        sb.AppendLine("          mkdir -p TestProject/ProjectSettings");
        sb.AppendLine();
        sb.AppendLine("      # Create manifest.json with package");
        sb.AppendLine("      - name: Create manifest");
        sb.AppendLine("        run: |");
        sb.AppendLine("          cat > TestProject/Packages/manifest.json << 'EOF'");
        sb.AppendLine("          {");
        sb.AppendLine("            \"dependencies\": {");
        sb.AppendLine($"              \"{packageName}\": \"file:../../package\"");
        sb.AppendLine("            }");
        sb.AppendLine("          }");
        sb.AppendLine("          EOF");
        sb.AppendLine();
        sb.AppendLine("      # Create ProjectSettings");
        sb.AppendLine("      - name: Create ProjectSettings");
        sb.AppendLine("        run: |");
        sb.AppendLine("          cat > TestProject/ProjectSettings/ProjectVersion.txt << 'EOF'");
        sb.AppendLine("          m_EditorVersion: ${{ matrix.unityVersion }}");
        sb.AppendLine("          EOF");
        sb.AppendLine();
        sb.AppendLine("      # Cache Unity Library");
        sb.AppendLine("      - name: Cache Library");
        sb.AppendLine("        uses: actions/cache@v3");
        sb.AppendLine("        with:");
        sb.AppendLine("          path: TestProject/Library");
        sb.AppendLine("          key: Library-TestProject-${{ matrix.unityVersion }}-${{ hashFiles('package/**') }}");
        sb.AppendLine("          restore-keys: |");
        sb.AppendLine("            Library-TestProject-${{ matrix.unityVersion }}-");
        sb.AppendLine("            Library-TestProject-");
        sb.AppendLine();
        sb.AppendLine("      # Run tests");
        sb.AppendLine("      - name: Run tests");
        sb.AppendLine("        uses: game-ci/unity-test-runner@v4");
        sb.AppendLine("        env:");
        sb.AppendLine("          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}");
        sb.AppendLine("          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}");
        sb.AppendLine("          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}");
        sb.AppendLine("        with:");
        sb.AppendLine("          projectPath: TestProject");
        sb.AppendLine("          unityVersion: ${{ matrix.unityVersion }}");
        sb.AppendLine("          githubToken: ${{ secrets.GITHUB_TOKEN }}");
        sb.AppendLine("          testMode: all");
        sb.AppendLine("          checkName: Test Results ${{ matrix.unityVersion }}");
        sb.AppendLine();
        sb.AppendLine("      # Upload test results");
        sb.AppendLine("      - name: Upload test results");
        sb.AppendLine("        uses: actions/upload-artifact@v3");
        sb.AppendLine("        if: always()");
        sb.AppendLine("        with:");
        sb.AppendLine("          name: Test results ${{ matrix.unityVersion }}");
        sb.AppendLine("          path: artifacts");

        return sb.ToString();
    }

    private string GenerateBuildWorkflow(string packageName, List<string> unityVersions, List<string> platforms)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("name: Build Package");
        sb.AppendLine();
        sb.AppendLine("on:");
        sb.AppendLine("  push:");
        sb.AppendLine("    branches: [ main ]");
        sb.AppendLine("  release:");
        sb.AppendLine("    types: [ created ]");
        sb.AppendLine("  workflow_dispatch:");
        sb.AppendLine();
        sb.AppendLine("jobs:");
        sb.AppendLine("  build:");
        sb.AppendLine("    name: Build ${{ matrix.targetPlatform }} (Unity ${{ matrix.unityVersion }})");
        sb.AppendLine("    runs-on: ${{ matrix.os }}");
        sb.AppendLine("    strategy:");
        sb.AppendLine("      fail-fast: false");
        sb.AppendLine("      matrix:");
        sb.AppendLine($"        unityVersion: [{string.Join(", ", unityVersions.Select(v => $"'{v}'"))}]");
        sb.AppendLine("        include:");
        
        foreach (var platform in platforms)
        {
            var os = GetOsForPlatform(platform);
            sb.AppendLine($"          - targetPlatform: {platform}");
            sb.AppendLine($"            os: {os}");
        }
        
        sb.AppendLine();
        sb.AppendLine("    steps:");
        sb.AppendLine("      # Checkout package");
        sb.AppendLine("      - name: Checkout package");
        sb.AppendLine("        uses: actions/checkout@v4");
        sb.AppendLine("        with:");
        sb.AppendLine("          path: package");
        sb.AppendLine();
        sb.AppendLine("      # Create test Unity project");
        sb.AppendLine("      - name: Create test project");
        sb.AppendLine("        run: |");
        sb.AppendLine("          mkdir -p TestProject/Assets");
        sb.AppendLine("          mkdir -p TestProject/Packages");
        sb.AppendLine("          mkdir -p TestProject/ProjectSettings");
        sb.AppendLine();
        sb.AppendLine("      # Create manifest.json");
        sb.AppendLine("      - name: Create manifest");
        sb.AppendLine("        shell: bash");
        sb.AppendLine("        run: |");
        sb.AppendLine("          cat > TestProject/Packages/manifest.json << 'EOF'");
        sb.AppendLine("          {");
        sb.AppendLine("            \"dependencies\": {");
        sb.AppendLine($"              \"{packageName}\": \"file:../../package\"");
        sb.AppendLine("            }");
        sb.AppendLine("          }");
        sb.AppendLine("          EOF");
        sb.AppendLine();
        sb.AppendLine("      # Create simple scene to test package");
        sb.AppendLine("      - name: Create test scene");
        sb.AppendLine("        shell: bash");
        sb.AppendLine("        run: |");
        sb.AppendLine("          mkdir -p TestProject/Assets/Scenes");
        sb.AppendLine("          echo '%YAML 1.1' > TestProject/Assets/Scenes/TestScene.unity");
        sb.AppendLine();
        sb.AppendLine("      # Create ProjectSettings");
        sb.AppendLine("      - name: Create ProjectSettings");
        sb.AppendLine("        shell: bash");
        sb.AppendLine("        run: |");
        sb.AppendLine("          cat > TestProject/ProjectSettings/ProjectVersion.txt << 'EOF'");
        sb.AppendLine("          m_EditorVersion: ${{ matrix.unityVersion }}");
        sb.AppendLine("          EOF");
        sb.AppendLine();
        sb.AppendLine("      # Cache Unity Library");
        sb.AppendLine("      - name: Cache Library");
        sb.AppendLine("        uses: actions/cache@v3");
        sb.AppendLine("        with:");
        sb.AppendLine("          path: TestProject/Library");
        sb.AppendLine("          key: Library-Build-${{ matrix.targetPlatform }}-${{ matrix.unityVersion }}");
        sb.AppendLine("          restore-keys: |");
        sb.AppendLine("            Library-Build-${{ matrix.targetPlatform }}-");
        sb.AppendLine("            Library-Build-");
        sb.AppendLine();
        sb.AppendLine("      # Build project");
        sb.AppendLine("      - name: Build project");
        sb.AppendLine("        uses: game-ci/unity-builder@v4");
        sb.AppendLine("        env:");
        sb.AppendLine("          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}");
        sb.AppendLine("          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}");
        sb.AppendLine("          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}");
        sb.AppendLine("        with:");
        sb.AppendLine("          projectPath: TestProject");
        sb.AppendLine("          unityVersion: ${{ matrix.unityVersion }}");
        sb.AppendLine("          targetPlatform: ${{ matrix.targetPlatform }}");
        sb.AppendLine("          buildName: PackageTest");
        sb.AppendLine();
        sb.AppendLine("      # Upload build");
        sb.AppendLine("      - name: Upload build");
        sb.AppendLine("        uses: actions/upload-artifact@v3");
        sb.AppendLine("        with:");
        sb.AppendLine("          name: Build-${{ matrix.targetPlatform }}-${{ matrix.unityVersion }}");
        sb.AppendLine("          path: build");

        return sb.ToString();
    }

    private string GenerateActivationWorkflow()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("name: Acquire Unity License");
        sb.AppendLine();
        sb.AppendLine("on:");
        sb.AppendLine("  workflow_dispatch:");
        sb.AppendLine();
        sb.AppendLine("jobs:");
        sb.AppendLine("  activation:");
        sb.AppendLine("    name: Request activation file");
        sb.AppendLine("    runs-on: ubuntu-latest");
        sb.AppendLine("    steps:");
        sb.AppendLine("      # Request manual activation file");
        sb.AppendLine("      - name: Request manual activation file");
        sb.AppendLine("        id: getManualLicenseFile");
        sb.AppendLine("        uses: game-ci/unity-request-activation-file@v2");
        sb.AppendLine();
        sb.AppendLine("      # Upload artifact");
        sb.AppendLine("      - name: Expose as artifact");
        sb.AppendLine("        uses: actions/upload-artifact@v3");
        sb.AppendLine("        with:");
        sb.AppendLine("          name: Unity_ALF");
        sb.AppendLine("          path: ${{ steps.getManualLicenseFile.outputs.filePath }}");

        return sb.ToString();
    }

    private int ShowSecretsInfo()
    {
        var panel = new Panel(new Markup(@"[yellow]GitHub Secrets Setup[/]

To use the generated workflows, add these secrets to your GitHub repository:

[cyan]1. Get Unity License[/]
   • Run the [green]activation.yml[/] workflow manually
   • Download the [green]Unity_ALF[/] artifact
   • Upload it to [blue]https://license.unity3d.com/manual[/]
   • Download the [green]Unity_v20XX.x.ulf[/] license file

[cyan]2. Add Repository Secrets[/]
   Go to: Settings → Secrets → Actions → New repository secret

   [yellow]UNITY_LICENSE[/]
   • Open the [green].ulf[/] file in a text editor
   • Copy entire contents
   • Paste as secret value

   [yellow]UNITY_EMAIL[/]
   • Your Unity account email
   
   [yellow]UNITY_PASSWORD[/]
   • Your Unity account password

[cyan]3. Trigger Workflows[/]
   • Push to main/develop branch
   • Create pull request
   • Run manually from Actions tab

[dim]For more info: https://game.ci/docs/github/activation[/]"))
            .BorderColor(Color.Yellow)
            .Header("[yellow]Setup Instructions[/]");

        AnsiConsole.Write(panel);
        return 0;
    }

    private int ShowUsage()
    {
        AnsiConsole.MarkupLine("[red]Invalid action[/]");
        AnsiConsole.MarkupLine("[yellow]Usage:[/]");
        AnsiConsole.MarkupLine("  pksmith ci generate [--package PATH] [--unity-versions 2022.3,2023.2]");
        AnsiConsole.MarkupLine("  pksmith ci add-secrets");
        return 1;
    }

    private List<string> ParseUnityVersions(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new List<string> { "2022.3", "2023.2" };
        }

        return input.Split(',').Select(v => v.Trim()).ToList();
    }

    private List<string> ParsePlatforms(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new List<string> 
            { 
                "StandaloneWindows64", 
                "StandaloneOSX", 
                "StandaloneLinux64" 
            };
        }

        return input.Split(',').Select(p => p.Trim()).ToList();
    }

    private string GetOsForPlatform(string platform)
    {
        return platform switch
        {
            "StandaloneOSX" => "macos-latest",
            "iOS" => "macos-latest",
            "StandaloneWindows64" => "windows-latest",
            "Android" => "ubuntu-latest",
            "WebGL" => "ubuntu-latest",
            "StandaloneLinux64" => "ubuntu-latest",
            _ => "ubuntu-latest"
        };
    }

    private void ShowNextSteps(bool simple)
    {
        var panel = new Panel(new Markup($@"[green]Next Steps:[/]

[cyan]1. Commit workflows[/]
   git add .github/workflows/
   git commit -m ""Add CI/CD workflows""
   git push

[cyan]2. Setup Unity License[/]
   Run: [yellow]pksmith ci add-secrets[/]
   Follow the instructions to add GitHub secrets

[cyan]3. Trigger workflows[/]
   • Push to main/develop branch
   • Or go to Actions tab and run manually

{(simple ? "" : @"[cyan]4. Check build results[/]
   • Tests run on every push/PR
   • Builds run on main branch and releases
   • Download artifacts from Actions tab")}

[dim]Documentation: https://game.ci/docs/github/getting-started[/]"))
            .BorderColor(Color.Green)
            .Header("[green]✓ Workflows Generated[/]");

        AnsiConsole.Write(panel);
    }
}
