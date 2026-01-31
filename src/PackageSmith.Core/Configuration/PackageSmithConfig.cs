using System.Text.Json.Serialization;

namespace PackageSmith.Core.Configuration;

[Serializable]
public struct PackageSmithConfig
{
    [JsonPropertyName("companyName")]
    public string CompanyName { get; set; }

    [JsonPropertyName("authorEmail")]
    public string AuthorEmail { get; set; }

    [JsonPropertyName("website")]
    public string Website { get; set; }

    [JsonPropertyName("defaultUnityVersion")]
    public string DefaultUnityVersion { get; set; }

    [JsonPropertyName("lastUpdatedTicks")]
    public long LastUpdatedTicks { get; set; }

    public readonly bool IsValid =>
        !string.IsNullOrWhiteSpace(CompanyName) &&
        !string.IsNullOrWhiteSpace(AuthorEmail) &&
        !string.IsNullOrWhiteSpace(DefaultUnityVersion);

    public readonly string ToDisplayString()
    {
        var now = new DateTime(LastUpdatedTicks, DateTimeKind.Utc);
        return $"Company: {CompanyName}\nEmail: {AuthorEmail}\nWebsite: {Website ?? "N/A"}\nUnity Version: {DefaultUnityVersion}\nLast Updated: {now:yyyy-MM-dd HH:mm:ss} UTC";
    }

    public static PackageSmithConfig GetDefault()
    {
        return new PackageSmithConfig
        {
            CompanyName = "YourCompany",
            AuthorEmail = "your.email@example.com",
            Website = null,
            DefaultUnityVersion = "2022.3",
            LastUpdatedTicks = DateTime.UtcNow.Ticks
        };
    }
}
