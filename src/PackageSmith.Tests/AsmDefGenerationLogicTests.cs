using System.Text.Json;
using System.Linq;
using PackageSmith.Core.Logic;

namespace PackageSmith.Tests;

public sealed class AsmDefGenerationLogicTests
{
	[Fact]
	public void GenerateJson_WithBasicInputs_ReturnsValidJson()
	{
		var result = AsmDefGenerationLogic.GenerateJson("Test.Assembly", [], false);

		var doc = JsonDocument.Parse(result);
		var root = doc.RootElement;

		Assert.Equal("Test.Assembly", root.GetProperty("name").GetString());
		Assert.False(root.GetProperty("allowUnsafeCode").GetBoolean());
	}

	[Fact]
	public void GenerateJson_WithReferences_IncludesReferences()
	{
		var references = new[]
		{
			new PackageSmith.Data.State.ReferenceState { Name = "Unity.Collections" },
			new PackageSmith.Data.State.ReferenceState { Name = "Unity.Mathematics" }
		};

		var result = AsmDefGenerationLogic.GenerateJson("Test.Assembly", references, false);

		var doc = JsonDocument.Parse(result);
		var root = doc.RootElement;
		var refs = root.GetProperty("references");

		Assert.True(refs.ValueKind == JsonValueKind.Array);
	}

	[Fact]
	public void GenerateJson_WithAllowUnsafe_SetsUnsafeFlag()
	{
		var result = AsmDefGenerationLogic.GenerateJson("Test.Assembly", [], true);

		var doc = JsonDocument.Parse(result);
		var root = doc.RootElement;

		Assert.True(root.GetProperty("allowUnsafeCode").GetBoolean());
	}

	[Fact]
	public void GenerateJson_WithVersionDefines_IncludesVersionDefines()
	{
		var versionDefines = new[]
		{
			new VersionDefine
			{
				Name = "com.unity.input.system",
				Expression = "1.7.0",
				Define = "UNITY_INPUT_SYSTEM_EXISTS"
			}
		};

		var result = AsmDefGenerationLogic.GenerateJson("Test.Assembly", [], false, versionDefines);

		var doc = JsonDocument.Parse(result);
		var root = doc.RootElement;
		var vd = root.GetProperty("versionDefines");

		Assert.True(vd.ValueKind == JsonValueKind.Array);
		Assert.Single(vd.EnumerateArray());
	}

	[Fact]
	public void GenerateEditorJson_SetsEditorPlatform()
	{
		var result = AsmDefGenerationLogic.GenerateEditorJson("Test.Assembly.Editor", []);

		var doc = JsonDocument.Parse(result);
		var root = doc.RootElement;
		var platforms = root.GetProperty("includePlatforms");

		Assert.True(platforms.ValueKind == JsonValueKind.Array);
		Assert.Equal("Editor", platforms.EnumerateArray().First().GetString());
	}

	[Fact]
	public void GenerateTestsJson_ExcludesEditorPlatform()
	{
		var result = AsmDefGenerationLogic.GenerateTestsJson("Test.Assembly.Tests", [], []);

		var doc = JsonDocument.Parse(result);
		var root = doc.RootElement;
		var platforms = root.GetProperty("excludePlatforms");

		Assert.True(platforms.ValueKind == JsonValueKind.Array);
		Assert.Equal("Editor", platforms.EnumerateArray().First().GetString());
	}

	[Fact]
	public void GenerateTestsJson_IncludesNunitReference()
	{
		var result = AsmDefGenerationLogic.GenerateTestsJson("Test.Assembly.Tests", [], []);

		var doc = JsonDocument.Parse(result);
		var root = doc.RootElement;
		var precompiled = root.GetProperty("precompiledReferences");

		Assert.True(precompiled.ValueKind == JsonValueKind.Array);
		Assert.Contains("nunit.framework.dll", precompiled.GetRawText());
	}

	[Fact]
	public void Unity2022_ReturnsXRVersionDefine()
	{
		var result = AsmDefGenerationLogic.Unity2022();

		Assert.Single(result);
		Assert.Equal("com.unity.modules.xr", result[0].Name);
		Assert.Equal("1.0.0", result[0].Expression);
		Assert.Equal("UNITY_XR_1_0_OR_NEWER", result[0].Define);
	}

	[Fact]
	public void Unity2023_ReturnsXRAndInputSystemVersionDefines()
	{
		var result = AsmDefGenerationLogic.Unity2023();

		Assert.Equal(2, result.Length);
		Assert.Contains(result, v => v.Name == "com.unity.modules.xr");
		Assert.Contains(result, v => v.Name == "com.unity.input.system");
	}

	[Fact]
	public void HDRP7_1_ReturnsHDRPVersionDefine()
	{
		var result = AsmDefGenerationLogic.HDRP7_1();

		Assert.Single(result);
		Assert.Equal("com.unity.render-pipelines.high-definition", result[0].Name);
		Assert.Equal("7.1.0", result[0].Expression);
		Assert.Equal("HDRP_7_1_0_OR_NEWER", result[0].Define);
	}

	[Fact]
	public void URP14_ReturnsURPVersionDefine()
	{
		var result = AsmDefGenerationLogic.URP14();

		Assert.Single(result);
		Assert.Equal("com.unity.render-pipelines.universal", result[0].Name);
		Assert.Equal("14.0.0", result[0].Expression);
		Assert.Equal("URP_14_0_OR_NEWER", result[0].Define);
	}

	[Fact]
	public void ParticleSystem1_0_ReturnsParticleSystemVersionDefine()
	{
		var result = AsmDefGenerationLogic.ParticleSystem1_0();

		Assert.Single(result);
		Assert.Equal("com.unity.modules.particlesystem", result[0].Name);
		Assert.Equal("USING_PARTICLE_SYSTEM", result[0].Define);
	}
}
