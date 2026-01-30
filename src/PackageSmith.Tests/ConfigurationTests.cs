using Xunit;
using PackageSmith.Core.Configuration;

namespace PackageSmith.Tests;

public sealed class ConfigurationTests
{
    [Fact]
    public void PackageNameValidator_ValidFormat_ReturnsTrue()
    {
        const string valid = "com.company.utilities";
        var result = PackageNameValidator.TryValidate(valid, out var error);
        Assert.True(result);
        Assert.Empty(error); // Empty string on success
    }

    [Theory]
    [InlineData("com.company.utilities", true)]
    [InlineData("io.gamestudio.networking", true)]
    [InlineData("org.opensource.framework", true)]
    [InlineData("Com.company.utilities", false)] // Uppercase not allowed
    [InlineData("com-company-utilities", false)] // Hyphens not allowed
    [InlineData("com.company", false)]           // Only 2 parts
    [InlineData("com company utilities", false)] // Spaces not allowed
    [InlineData("", false)]                       // Empty
    [InlineData("com..company", false)]          // Double dot
    public void PackageNameValidator_VariousInputs(string input, bool expected)
    {
        var result = PackageNameValidator.TryValidate(input, out var error);
        Assert.Equal(expected, result);

        if (!expected)
        {
            Assert.NotNull(error);
            Assert.NotEmpty(error);
        }
    }

    [Fact]
    public void PackageModule_FlagsEnum_WorksCorrectly()
    {
        var runtimeAndEditor = PackageModule.Runtime | PackageModule.Editor;
        Assert.True(runtimeAndEditor.HasFlag(PackageModule.Runtime));
        Assert.True(runtimeAndEditor.HasFlag(PackageModule.Editor));
        Assert.False(runtimeAndEditor.HasFlag(PackageModule.Tests));
    }

    [Fact]
    public void PackageModuleExtensions_ToFolderName_ReturnsCorrectNames()
    {
        Assert.Equal("Runtime", PackageModule.Runtime.ToFolderName());
        Assert.Equal("Editor", PackageModule.Editor.ToFolderName());
        Assert.Equal("Tests", PackageModule.Tests.ToFolderName());
        Assert.Equal("Samples", PackageModule.Samples.ToFolderName());
    }

    [Fact]
    public void PackageSmithConfig_Default_IsInvalid()
    {
        var config = default(PackageSmithConfig);
        Assert.False(config.IsValid);
    }

    [Fact]
    public void PackageSmithConfig_WithRequiredFields_IsValid()
    {
        var config = new PackageSmithConfig
        {
            CompanyName = "Test Company",
            AuthorEmail = "test@example.com",
            Website = "https://example.com",
            DefaultUnityVersion = "2022.3"
        };
        Assert.True(config.IsValid);
    }

    [Fact]
    public void PackageTemplate_Default_IsInvalid()
    {
        var template = default(PackageTemplate);
        Assert.False(template.IsValid);
    }

    [Fact]
    public void PackageTemplate_WithName_IsValid()
    {
        var template = new PackageTemplate
        {
            PackageName = "com.test.package"
        };
        Assert.True(template.IsValid);
    }
}
