using System.Text.Json;
using System.Runtime.InteropServices;

namespace PackageSmith.Core.Configuration;

public sealed class ConfigService : IConfigService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _configDirectory;
    private readonly string _configPath;

    public ConfigService()
    {
        _configDirectory = GetAppDataPath();
        _configPath = Path.Combine(_configDirectory, "PackageSmith", "config.json");

        EnsureDirectoryExists();
    }

    public bool ConfigExists() => File.Exists(_configPath);

    public string GetConfigPath() => _configPath;

    public bool TryLoadConfig(out PackageSmithConfig config)
    {
        config = default;

        if (!ConfigExists()) return false;

        try
        {
            var json = File.ReadAllText(_configPath);
            var loaded = JsonSerializer.Deserialize<PackageSmithConfig>(json, _jsonOptions);

            if (loaded is { IsValid: true })
            {
                config = loaded;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public PackageSmithConfig LoadConfigOrDefault()
    {
        return TryLoadConfig(out var config) ? config : PackageSmithConfig.GetDefault();
    }

    public bool TrySaveConfig(in PackageSmithConfig config)
    {
        try
        {
            var toSave = config;
            toSave.LastUpdatedTicks = DateTime.UtcNow.Ticks;

            var json = JsonSerializer.Serialize(toSave, _jsonOptions);
            File.WriteAllText(_configPath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TryDeleteConfig()
    {
        try
        {
            if (ConfigExists())
            {
                File.Delete(_configPath);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string GetAppDataPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_configDirectory))
        {
            Directory.CreateDirectory(_configDirectory);
        }

        var packageSmithDir = Path.Combine(_configDirectory, "PackageSmith");
        if (!Directory.Exists(packageSmithDir))
        {
            Directory.CreateDirectory(packageSmithDir);
        }
    }
}
