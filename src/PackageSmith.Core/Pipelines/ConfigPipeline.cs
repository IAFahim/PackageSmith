using System.IO;
using PackageSmith.Core.Extensions;
using PackageSmith.Core.Interfaces;
using PackageSmith.Core.Logic;
using PackageSmith.Data.Config;

namespace PackageSmith.Core.Pipelines;

public sealed class ConfigPipeline : IConfigService
{
    private readonly string _configPath;

    public ConfigPipeline()
    {
        FileSystemLogic.GetAppDataPath(out var appDataPath);
        _configPath = Path.Combine(appDataPath, "PackageSmith", "config.json");
        EnsureDirectory();
    }

    public bool ConfigExists(string configPath)
    {
        FileSystemLogic.FileExists(configPath, out var exists);
        return exists;
    }

    public bool TryLoad(string configPath, out AppConfig config)
    {
        config = default;
        var temp = config;
        return temp.TryLoad(configPath, out _);
    }

    public bool TrySave(in AppConfig config, string configPath)
    {
        var temp = config;
        return temp.TrySave(configPath, out _);
    }

    public bool TryDelete(string configPath)
    {
        try
        {
            FileSystemLogic.FileExists(configPath, out var exists);
            if (exists) FileSystemLogic.DeleteFile(configPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void GetDefault(out AppConfig config)
    {
        ConfigExtensions.TryGetDefault(out config);
    }

    public void GetConfigPath(out string configPath)
    {
        configPath = _configPath;
    }

    private void EnsureDirectory()
    {
        var dir = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(dir)) dir.TryEnsureDirectory();
    }
}