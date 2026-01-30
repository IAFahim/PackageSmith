using System.Text.RegularExpressions;

namespace PackageSmith.Core.Configuration;

public static class PackageNameValidator
{
    private static readonly Regex ReverseDomainRegex = new(
        @"^[a-z]+\.[a-z]+(\.[a-z]+)+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    public static bool IsValidPackageName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        return ReverseDomainRegex.IsMatch(name.Trim());
    }

    public static bool TryValidate(string name, out string error)
    {
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Package name cannot be empty";
            return false;
        }

        if (name.Contains(' '))
        {
            error = "Package name cannot contain spaces";
            return false;
        }

        if (!ReverseDomainRegex.IsMatch(name.Trim()))
        {
            error = "Package name must follow reverse domain notation (e.g., com.company.feature)";
            return false;
        }

        return true;
    }

    public static string[] GetExamples() => new[]
    {
        "com.mycompany.utilities",
        "io.gamestudio.networking",
        "dev.author.toolkit",
        "org.industry.physics"
    };
}
