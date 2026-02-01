using System;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using PackageSmith.Data.Types;

namespace PackageSmith.Data.Config;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct AppConfig
{
	[JsonPropertyName("companyName")]
	public string CompanyName;

	[JsonPropertyName("authorEmail")]
	public string AuthorEmail;

	[JsonPropertyName("website")]
	public string Website;

	[JsonPropertyName("defaultUnityVersion")]
	public string DefaultUnityVersion;

	[JsonPropertyName("lastUpdatedTicks")]
	public long LastUpdatedTicks;

	public readonly override string ToString() => $"[Config] {CompanyName} | {DefaultUnityVersion}";
}
