using System;
using System.Runtime.CompilerServices;
using PackageSmith.Data.Config;
using PackageSmith.Data.Types;

namespace PackageSmith.Core.Logic;

public static class ConfigLogic
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidateCompanyName(in string companyName, out bool isValid)
	{
		isValid = !string.IsNullOrWhiteSpace(companyName) && companyName.Length > 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidateEmail(in string email, out bool isValid)
	{
		isValid = !string.IsNullOrWhiteSpace(email) && email.Contains('@');
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidateUnityVersion(in string version, out bool isValid)
	{
		isValid = !string.IsNullOrWhiteSpace(version);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void IsConfigValid(in AppConfig config, out bool isValid)
	{
		ValidateCompanyName(config.CompanyName, out var nameValid);
		ValidateEmail(config.AuthorEmail, out var emailValid);
		ValidateUnityVersion(config.DefaultUnityVersion, out var versionValid);
		isValid = nameValid && emailValid && versionValid;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GetDefaultCompany(out string companyName)
	{
		companyName = "YourCompany";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GetDefaultEmail(out string email)
	{
		email = "your.email@example.com";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GetDefaultUnityVersion(out string version)
	{
		version = "2022.3";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GetCurrentTimeTicks(out long ticks)
	{
		ticks = DateTime.UtcNow.Ticks;
	}
}
