using Xunit;
using PackageSmith.Core.Configuration;
using PackageSmith.Core.Generation;
using PackageSmith.Core.AssemblyDefinition;
using PackageSmith.Core.Dependencies;

namespace PackageSmith.Tests;

public sealed class AsmDefNamingTests
{
    [Fact]
    public void PackageGenerator_UsesCleanAsmDefNames_NotFullPackageName()
    {
        // Arrange
        var config = new PackageSmithConfig
        {
            CompanyName = "Test Company",
            AuthorEmail = "test@test.com",
            Website = "https://test.com",
            DefaultUnityVersion = "2022.3"
        };

        var template = new PackageTemplate
        {
            PackageName = "com.intents.intent",
            DisplayName = "Intent System",
            Description = "Test Package",
            OutputPath = "/tmp/test-output",
            SelectedModules = PackageModule.Runtime | PackageModule.Editor | PackageModule.Tests,
            SubAssemblies = SubAssemblyType.Data | SubAssemblyType.Authoring,
            EnableSubAssemblies = true,
            Dependencies = Array.Empty<PackageDependency>()
        };

        // Act
        var generator = new PackageGenerator();
        var success = generator.TryGenerate(in template, in config, out var layout);

        // Assert
        Assert.True(success);
        
        var asmdefFiles = layout.Files.Where(f => f.Path.EndsWith(".asmdef")).ToList();
        Assert.NotEmpty(asmdefFiles);

        // Check that asmdef files use "Intent.*" not "com.intents.intent.*"
        foreach (var file in asmdefFiles)
        {
            var filename = Path.GetFileName(file.Path);
            
            // Should start with "Intent" (the extracted root), not "com."
            Assert.True(
                filename.StartsWith("Intent.") || filename == "Intent.asmdef",
                $"Expected asmdef filename to start with 'Intent', but got: {filename}"
            );
            
            // Should NOT contain the full package name
            Assert.DoesNotContain("com.intents.intent", filename);
        }
    }

    [Theory]
    [InlineData("com.intents.intent", "Intent")]
    [InlineData("io.github.player", "Player")]
    [InlineData("org.example.physics", "Physics")]
    public void GetAsmDefRootFromPackageName_ExtractsLastSegment(string packageName, string expectedRoot)
    {
        var result = NamespaceGenerator.GetAsmDefRootFromPackageName(packageName);
        Assert.Equal(expectedRoot, result);
    }
}
