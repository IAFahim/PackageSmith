namespace PackageSmith.Core.Configuration;

public interface IConfigService
{
    bool TryLoadConfig(out PackageSmithConfig config);
    bool TrySaveConfig(in PackageSmithConfig config);
    string GetConfigPath();
    bool ConfigExists();
    bool TryDeleteConfig();
}
