using System;
using System.Text.Json;
using PackageSmith.Data.Config;
using PackageSmith.Data.Types;
using PackageSmith.Core.Logic;

namespace PackageSmith.Core.Extensions;

public static class ConfigExtensions
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public static bool TryLoad(this ref AppConfig config, string configPath, out bool success)
	{
		success = false;

		FileSystemLogic.FileExists(configPath, out var exists);
		if (!exists) return false;

		try
		{
			FileSystemLogic.ReadFile(configPath, out var json);
			var loaded = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
			config = loaded;

			ConfigLogic.IsConfigValid(in config, out var isValid);
			if (!isValid) return false;

			success = true;
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool TrySave(this ref AppConfig config, string configPath, out bool success)
	{
		success = false;

		try
		{
			var toSave = config;
			ConfigLogic.GetCurrentTimeTicks(out var ticks);
			toSave.LastUpdatedTicks = ticks;

			var json = JsonSerializer.Serialize(toSave, JsonOptions);
			FileSystemLogic.WriteFile(configPath, json);
			success = true;
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool TryGetDefault(out AppConfig config)
	{
		ConfigLogic.GetDefaultCompany(out var company);
		ConfigLogic.GetDefaultEmail(out var email);
		ConfigLogic.GetDefaultUnityVersion(out var unityVersion);
		ConfigLogic.GetCurrentTimeTicks(out var ticks);

		config = new AppConfig
		{
			CompanyName = company,
			AuthorEmail = email,
			Website = new FixedString64(string.Empty),
			DefaultUnityVersion = unityVersion,
			LastUpdatedTicks = ticks
		};

		return true;
	}

	public static bool TryValidate(this ref AppConfig config, out bool isValid)
	{
		ConfigLogic.IsConfigValid(in config, out isValid);
		return isValid;
	}
}
