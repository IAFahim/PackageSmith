using PackageSmith.Core.Logic;

namespace PackageSmith.Tests;

public sealed class TemplateGeneratorLogicTests : IDisposable
{
	private readonly string _testDir;
	private readonly string _templateDir;

	public TemplateGeneratorLogicTests()
	{
		_testDir = Path.Combine(Path.GetTempPath(), $"pksmith_test_{Guid.NewGuid()}");
		_templateDir = Path.Combine(Path.GetTempPath(), $"pksmith_template_{Guid.NewGuid()}");
		Directory.CreateDirectory(_testDir);
		Directory.CreateDirectory(_templateDir);
	}

	public void Dispose()
	{
		if (Directory.Exists(_testDir))
			Directory.Delete(_testDir, true);
		if (Directory.Exists(_templateDir))
			Directory.Delete(_templateDir, true);
	}

	[Fact]
	public void TryGenerateFromTemplate_WithNonExistentTemplate_ReturnsFalse()
	{
		var result = TemplateGeneratorLogic.TryGenerateFromTemplate(
			"/nonexistent/path",
			_testDir,
			"com.test.package",
			out var processedFiles
		);

		Assert.False(result);
		Assert.Equal(0, processedFiles);
	}

	[Fact]
	public void TryGenerateFromTemplate_WithExistingNonEmptyDir_ReturnsFalse()
	{
		File.WriteAllText(Path.Combine(_testDir, "test.txt"), "content");

		var result = TemplateGeneratorLogic.TryGenerateFromTemplate(
			_templateDir,
			_testDir,
			"com.test.package",
			out var processedFiles
		);

		Assert.False(result);
	}

	[Fact]
	public void TryGenerateFromTemplate_WithEmptyDir_ReturnsTrue()
	{
		File.WriteAllText(Path.Combine(_templateDir, "test.txt"), "content");

		var result = TemplateGeneratorLogic.TryGenerateFromTemplate(
			_templateDir,
			_testDir,
			"com.test.package",
			out var processedFiles
		);

		Assert.True(result);
		Assert.Equal(1, processedFiles);
	}

	[Fact]
	public void TryGenerateFromTemplate_DetokenizesPackageName()
	{
		File.WriteAllText(
			Path.Combine(_templateDir, "package.json"),
			@"{ ""name"": ""{{PACKAGE_NAME}}"" }"
		);

		TemplateGeneratorLogic.TryGenerateFromTemplate(
			_templateDir,
			_testDir,
			"com.test.newpackage",
			out var _
		);

		var result = File.ReadAllText(Path.Combine(_testDir, "package.json"));
		Assert.Contains("com.test.newpackage", result);
	}

	[Fact]
	public void TryGenerateFromTemplate_SanitizesHyphensInAssemblyName()
	{
		Directory.CreateDirectory(Path.Combine(_templateDir, "{{ASM_NAME}}"));
		File.WriteAllText(
			Path.Combine(_templateDir, "{{ASM_NAME}}", "{{ASM_NAME}}.asmdef"),
			@"{ ""name"": ""{{ASM_NAME}}"" }"
		);

		TemplateGeneratorLogic.TryGenerateFromTemplate(
			_templateDir,
			_testDir,
			"com.my-company.tool",
			out var _
		);

		var resultDir = Directory.GetDirectories(_testDir)[0];
		var dirName = Path.GetFileName(resultDir);
		Assert.Equal("MyCompany.Tool", dirName);

		var asmdefFiles = Directory.GetFiles(resultDir, "*.asmdef");
		Assert.NotEmpty(asmdefFiles);
		var content = File.ReadAllText(asmdefFiles[0]);
		Assert.Contains("MyCompany.Tool", content);
	}

	[Fact]
	public void TryGenerateFromTemplate_SanitizesMultipleHyphens()
	{
		Directory.CreateDirectory(Path.Combine(_templateDir, "{{ASM_NAME}}"));
		File.WriteAllText(
			Path.Combine(_templateDir, "{{ASM_NAME}}", "{{ASM_NAME}}.cs"),
			"namespace {{ASM_SHORT_NAME}} {}"
		);

		TemplateGeneratorLogic.TryGenerateFromTemplate(
			_templateDir,
			_testDir,
			"com.my-awesome-cool-tool",
			out var _
		);

		var csFiles = Directory.GetFiles(_testDir, "*.cs", SearchOption.AllDirectories);
		Assert.NotEmpty(csFiles);
		var content = File.ReadAllText(csFiles[0]);
		Assert.Contains("Tool", content);
	}

	[Fact]
	public void TryGenerateFromTemplate_PreservesDotNotation()
	{
		Directory.CreateDirectory(Path.Combine(_templateDir, "{{ASM_NAME}}"));
		File.WriteAllText(
			Path.Combine(_templateDir, "{{ASM_NAME}}", "{{ASM_NAME}}.asmdef"),
			@"{ ""name"": ""{{ASM_NAME}}"" }"
		);

		TemplateGeneratorLogic.TryGenerateFromTemplate(
			_templateDir,
			_testDir,
			"com.test.sub.feature",
			out var _
		);

		var asmdefFiles = Directory.GetFiles(_testDir, "*.asmdef", SearchOption.AllDirectories);
		Assert.NotEmpty(asmdefFiles);
		var content = File.ReadAllText(asmdefFiles[0]);
		Assert.Contains("Test.Sub.Feature", content);
	}

	[Fact]
	public void TryGenerateFromTemplate_ProcessesAsmDefWithVersionDefines()
	{
		Directory.CreateDirectory(Path.Combine(_templateDir, "{{ASM_NAME}}"));
		File.WriteAllText(
			Path.Combine(_templateDir, "{{ASM_NAME}}", "{{ASM_NAME}}.asmdef"),
			@"{
				""name"": ""{{ASM_NAME}}"",
				""references"": [""Unity.InputSystem"", ""Unity.Collections""]
			}"
		);

		TemplateGeneratorLogic.TryGenerateFromTemplate(
			_templateDir,
			_testDir,
			"com.test.input",
			out var _
		);

		var asmdefFiles = Directory.GetFiles(_testDir, "*.asmdef", SearchOption.AllDirectories);
		Assert.NotEmpty(asmdefFiles);
		var content = File.ReadAllText(asmdefFiles[0]);
		Assert.Contains("Test.Input", content);
		Assert.Contains("versionDefines", content);
	}

	[Fact]
	public void TryGenerateFromTemplate_SkipsDotFiles()
	{
		File.WriteAllText(Path.Combine(_templateDir, ".gitignore"), "*.meta");
		File.WriteAllText(Path.Combine(_templateDir, "README.md"), "# Test");

		TemplateGeneratorLogic.TryGenerateFromTemplate(
			_templateDir,
			_testDir,
			"com.test.package",
			out var processedFiles
		);

		Assert.Equal(1, processedFiles);
		Assert.False(File.Exists(Path.Combine(_testDir, ".gitignore")));
		Assert.True(File.Exists(Path.Combine(_testDir, "README.md")));
	}
}
