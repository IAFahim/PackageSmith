using System;
using System.Runtime.CompilerServices;
using PackageSmith.Data.Config;
using PackageSmith.Data.Types;

namespace PackageSmith.Core.Logic;

public static class ConfigLogic
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidateCompanyName(in FixedString64 companyName, out bool isValid)
	{
		var str = companyName.ToString();
		isValid = !string.IsNullOrWhiteSpace(str) && str.Length > 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidateEmail(in FixedString64 email, out bool isValid)
	{
		var str = email.ToString();
		isValid = !string.IsNullOrWhiteSpace(str) && str.Contains('@');
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidateUnityVersion(in FixedString64 version, out bool isValid)
	{
		var str = version.ToString();
		isValid = !string.IsNullOrWhiteSpace(str);
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
	public static void GetDefaultCompany(out FixedString64 companyName)
	{
		companyName = new FixedString64("YourCompany");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GetDefaultEmail(out FixedString64 email)
	{
		email = new FixedString64("your.email@example.com");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GetDefaultUnityVersion(out FixedString64 version)
	{
		version = new FixedString64("2022.3");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GetCurrentTimeTicks(out long ticks)
	{
		ticks = DateTime.UtcNow.Ticks;
	}
}
