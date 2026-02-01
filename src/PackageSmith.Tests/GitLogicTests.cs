using PackageSmith.Core.Logic;
using PackageSmith.Data.Types;

namespace PackageSmith.Tests;

public sealed class GitLogicTests
{
	[Fact]
	public void GenerateGitIgnore_ReturnsNonEmptyString()
	{
		var result = GitLogic.GenerateGitIgnore();

		Assert.NotEmpty(result);
		Assert.Contains("Unity", result);
	}

	[Fact]
	public void GenerateGitIgnore_ContainsUnitySpecificPatterns()
	{
		var result = GitLogic.GenerateGitIgnore();

		Assert.Contains("/[Ll]ibrary/", result);
		Assert.Contains("/[Oo]bj/", result);
		Assert.Contains("/[Bb]uild/", result);
		Assert.Contains(".DS_Store", result);
	}

	[Fact]
	public void GenerateGitIgnore_ContainsIdePatterns()
	{
		var result = GitLogic.GenerateGitIgnore();

		Assert.Contains(".idea", result);
		Assert.Contains(".vscode", result);
		Assert.Contains("*.csproj", result);
		Assert.Contains("*.sln", result);
	}

	[Fact]
	public void TryGetGitConfig_ReturnsValidStrings()
	{
		GitLogic.TryGetGitConfig(out var userName, out var userEmail);

		Assert.NotNull(userName);
		Assert.NotNull(userEmail);
		Assert.NotEmpty(userName);
	}

	[Fact]
	public void GenerateLicense_WithMit_ReturnsMitLicense()
	{
		var result = GitLogic.GenerateLicense(LicenseType.Mit, "2026", "TestCompany");

		Assert.Contains("MIT License", result);
		Assert.Contains("2026", result);
		Assert.Contains("TestCompany", result);
		Assert.Contains("Permission is hereby granted", result);
	}

	[Fact]
	public void GenerateLicense_WithApache20_ReturnsApacheLicense()
	{
		var result = GitLogic.GenerateLicense(LicenseType.Apache20, "2026", "TestCompany");

		Assert.Contains("Apache License", result);
		Assert.Contains("Version 2.0", result);
		Assert.Contains("2026", result);
		Assert.Contains("TestCompany", result);
	}

	[Fact]
	public void GenerateLicense_WithProprietary_ReturnsProprietaryLicense()
	{
		var result = GitLogic.GenerateLicense(LicenseType.Proprietary, "2026", "TestCompany");

		Assert.Contains("PROPRIETARY LICENSE", result);
		Assert.Contains("2026", result);
		Assert.Contains("TestCompany", result);
		Assert.Contains("All rights reserved", result);
	}

	[Fact]
	public void GenerateLicense_WithNone_ReturnsEmptyString()
	{
		var result = GitLogic.GenerateLicense(LicenseType.None, "2026", "TestCompany");

		Assert.Empty(result);
	}

	[Fact]
	public void GenerateChangelog_ContainsPackageName()
	{
		var result = GitLogic.GenerateChangelog("com.test.package", "1.0.0");

		Assert.Contains("com.test.package", result);
		Assert.Contains("1.0.0", result);
	}

	[Fact]
	public void GenerateChangelog_ContainsInitialRelease()
	{
		var result = GitLogic.GenerateChangelog("com.test.package");

		Assert.Contains("Initial release", result);
		Assert.Contains("Added", result);
	}

	[Fact]
	public void GenerateChangelog_ContainsCorrectDateFormat()
	{
		var result = GitLogic.GenerateChangelog("com.test.package");
		var expectedDate = DateTime.Now.ToString("yyyy-MM-dd");

		Assert.Contains(expectedDate, result);
	}
}
