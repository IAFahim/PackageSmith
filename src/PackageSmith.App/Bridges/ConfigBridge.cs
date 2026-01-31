using PackageSmith.Data.Config;
using PackageSmith.Core.Pipelines;

namespace PackageSmith.App.Bridges;

public sealed class ConfigBridge : IConfigBridge
{
	private readonly ConfigPipeline _pipeline;

	public ConfigBridge()
	{
		_pipeline = new ConfigPipeline();
	}

	public bool TryLoad(out AppConfig config)
	{
		var path = _pipeline.GetConfigPath();
		return _pipeline.TryLoad(path, out config);
	}

	public bool TrySave(in AppConfig config)
	{
		var path = _pipeline.GetConfigPath();
		return _pipeline.TrySave(in config, path);
	}

	public bool TryDelete()
	{
		var path = _pipeline.GetConfigPath();
		return _pipeline.TryDelete(path);
	}

	public AppConfig GetDefault()
	{
		return _pipeline.GetDefault();
	}
}
