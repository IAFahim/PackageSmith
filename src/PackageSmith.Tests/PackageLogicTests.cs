using PackageSmith.Core.Logic;
using PackageSmith.Core.Extensions;
using PackageSmith.Data.State;
using PackageSmith.Data.Types;

namespace PackageSmith.Tests;

public sealed class PackageLogicTests
{
	[Fact]
	public void HasModule_WithRuntimeModule_ReturnsTrue()
	{
		var package = new PackageState
		{
			PackageName = "com.test.package",
			SelectedModules = PackageModuleType.Runtime
		};

		var result = package.HasModule(PackageModuleType.Runtime);

		Assert.True(result);
	}

	[Fact]
	public void HasModule_WithMissingModule_ReturnsFalse()
	{
		var package = new PackageState
		{
			PackageName = "com.test.package",
			SelectedModules = PackageModuleType.Editor
		};

		var result = package.HasModule(PackageModuleType.Runtime);

		Assert.False(result);
	}

	[Fact]
	public void IsEcsEnabled_WithEntitiesEnabled_ReturnsTrue()
	{
		var package = new PackageState
		{
			PackageName = "com.test.package",
			EcsPreset = new EcsPresetState
			{
				EnableEntities = true
			}
		};

		var result = package.IsEcsEnabled();

		Assert.True(result);
	}

	[Fact]
	public void IsEcsEnabled_WithEntitiesDisabled_ReturnsFalse()
	{
		var package = new PackageState
		{
			PackageName = "com.test.package",
			EcsPreset = new EcsPresetState
			{
				EnableEntities = false
			}
		};

		var result = package.IsEcsEnabled();

		Assert.False(result);
	}

	[Theory]
	[InlineData("com.company.package", "package")]
	[InlineData("com.test.sub.feature", "feature")]
	[InlineData("com.my-tool", "tool")]
	[InlineData("com.my-awesome-tool", "tool")]
	[InlineData("com.single", "single")]
	public void GetAsmDefRoot_VariousPackageNames_ReturnsCorrectRoot(string packageName, string expectedRoot)
	{
		var package = new PackageState
		{
			PackageName = packageName
		};

		package.TryGetAsmDefRoot(out var root);

		Assert.Equal(expectedRoot, root);
	}

	[Theory]
	[InlineData("com.company.package", "company")]
	[InlineData("com.test.sub.feature", "test.sub")]
	[InlineData("com.my-tool", "my-tool")]
	[InlineData("com.my-awesome-tool", "my-awesome-tool")]
	[InlineData("com.single", "single")]
	public void GenerateNamespace_VariousPackageNames_ReturnsCorrectNamespace(string packageName, string expectedNamespace)
	{
		PackageLogic.GenerateNamespace(packageName, out var ns);

		Assert.Equal(expectedNamespace, ns);
	}

	[Theory]
	[InlineData("com.company.package", true, true, false, false)]
	[InlineData("com.test.minimal", false, false, false, false)]
	[InlineData("com.test.full", true, true, true, true)]
	public void HasModule_WithModuleFlags_ReturnsExpectedResult(
		string packageName,
		bool hasRuntime,
		bool hasEditor,
		bool hasTests,
		bool hasSamples)
	{
		var modules = PackageModuleType.None;
		if (hasRuntime) modules |= PackageModuleType.Runtime;
		if (hasEditor) modules |= PackageModuleType.Editor;
		if (hasTests) modules |= PackageModuleType.Tests;
		if (hasSamples) modules |= PackageModuleType.Samples;

		var package = new PackageState
		{
			PackageName = packageName,
			SelectedModules = modules
		};

		Assert.Equal(hasRuntime, package.HasModule(PackageModuleType.Runtime));
		Assert.Equal(hasEditor, package.HasModule(PackageModuleType.Editor));
		Assert.Equal(hasTests, package.HasModule(PackageModuleType.Tests));
		Assert.Equal(hasSamples, package.HasModule(PackageModuleType.Samples));
	}

	[Fact]
	public void GetNamespace_WithValidPackage_ReturnsValidNamespace()
	{
		var package = new PackageState
		{
			PackageName = "com.test.package"
		};

		var result = package.TryGetNamespace(out var ns);

		Assert.True(result);
		Assert.Equal("Test.Package", ns);
	}

	[Fact]
	public void TryGetBasePath_WithValidPackage_ReturnsValidPath()
	{
		var package = new PackageState
		{
			PackageName = "com.test.package",
			OutputPath = "/test"
		};

		var result = package.TryGetBasePath(out var path);

		Assert.True(result);
		Assert.Contains("com.test.package", path);
	}
}
