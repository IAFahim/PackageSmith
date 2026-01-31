namespace PackageSmith.Core.Configuration;

public interface IConfigService
{
    bool TryLoadConfig(out PackageSmithConfig config);
    PackageSmithConfig LoadConfigOrDefault();
    bool TrySaveConfig(in PackageSmithConfig config);
    string GetConfigPath();
    bool ConfigExists();
    bool TryDeleteConfig();
}
