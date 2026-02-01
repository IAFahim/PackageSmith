using PackageSmith.Data.Config;

namespace PackageSmith.Core.Interfaces;

public interface IConfigService
{
	bool ConfigExists(string configPath);
	bool TryLoad(string configPath, out AppConfig config);
	bool TrySave(in AppConfig config, string configPath);
	bool TryDelete(string configPath);
	void GetDefault(out AppConfig config);
	void GetConfigPath(out string configPath);
}
