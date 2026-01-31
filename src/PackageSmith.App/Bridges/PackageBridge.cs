using PackageSmith.Data.Config;
using PackageSmith.Data.State;
using PackageSmith.Core.Pipelines;

namespace PackageSmith.App.Bridges;

public sealed class PackageBridge : IPackageBridge
{
	private readonly BuildPipeline _buildPipeline;
	private readonly FileSystemPipeline _fileSystemPipeline;

	public PackageBridge()
	{
		_buildPipeline = new BuildPipeline();
		_fileSystemPipeline = new FileSystemPipeline();
	}

	public bool TryCreate(in PackageState package)
	{
		var bridge = new ConfigBridge();
		var config = bridge.TryLoad(out var c) ? c : bridge.GetDefault();

		return _buildPipeline.TryGenerate(in package, in config, out var layout);
	}
}
