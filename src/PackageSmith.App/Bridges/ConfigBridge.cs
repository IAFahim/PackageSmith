using PackageSmith.Data.Config;
using PackageSmith.Core.Pipelines;

namespace PackageSmith.App.Bridges;

public sealed class ConfigBridge : IConfigBridge
{
	private readonly ConfigPipeline _pipeline = new();

	public bool TryLoad(out AppConfig config)
	{
		_pipeline.GetConfigPath(out var path);
		return _pipeline.TryLoad(path, out config);
	}

	public bool TrySave(in AppConfig config)
	{
		_pipeline.GetConfigPath(out var path);
		return _pipeline.TrySave(in config, path);
	}

	public bool TryDelete()
	{
		_pipeline.GetConfigPath(out var path);
		return _pipeline.TryDelete(path);
	}

	public AppConfig GetDefault()
	{
		_pipeline.GetDefault(out var config);
		return config;
	}
}
