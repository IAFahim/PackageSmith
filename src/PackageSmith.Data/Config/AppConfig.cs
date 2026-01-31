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
	public FixedString64 CompanyName;

	[JsonPropertyName("authorEmail")]
	public FixedString64 AuthorEmail;

	[JsonPropertyName("website")]
	public FixedString64 Website;

	[JsonPropertyName("defaultUnityVersion")]
	public FixedString64 DefaultUnityVersion;

	[JsonPropertyName("lastUpdatedTicks")]
	public long LastUpdatedTicks;

	public override readonly string ToString() => $"[Config] {CompanyName.ToString()} | {DefaultUnityVersion.ToString()}";
}
