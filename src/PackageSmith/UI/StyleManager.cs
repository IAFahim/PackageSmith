using Spectre.Console;

namespace PackageSmith.UI;

public static class StyleManager
{
    // Colors
    public static readonly Color CommandColor = Color.Teal;
    public static readonly Color WarningColor = Color.Orange1;
    public static readonly Color ErrorColor = Color.Red;
    public static readonly Color SuccessColor = Color.Green;
    public static readonly Color PathColor = Color.SlateBlue1;
    public static readonly Color InfoColor = Color.CornflowerBlue;
    public static readonly Color MutedColor = Color.Grey;
    public static readonly Color AccentColor = Color.Purple;

    // Icons
    public const string IconPackage = "📦";
    public const string IconConfig = "🔧";
    public const string IconDeploy = "🚀";
    public const string IconCode = "📝";
    public const string IconFolder = "📁";
    public const string IconSuccess = "✅";
    public const string IconError = "❌";
    public const string IconWarning = "⚠️";
    public const string IconInfo = "ℹ️";
    public const string IconGit = "🔀";
    public const string IconTemplate = "📋";
    public const string IconDependency = "📎";
    public const string IconTest = "🧪";

    // Styles
    public static Style Command => new Style(CommandColor);
    public static Style Warning => new Style(WarningColor);
    public static Style Error => new Style(ErrorColor);
    public static Style Success => new Style(SuccessColor);
    public static Style Path => new Style(PathColor);
    public static Style Info => new Style(InfoColor);
    public static Style Muted => new Style(MutedColor);
    public static Style Accent => new Style(AccentColor);

    // Formatted markup helpers
    public static string Package(string text) => $"[{CommandColor.ToMarkup()}]{IconPackage} {text}[/]";
    public static string SuccessText(string text) => $"[{SuccessColor.ToMarkup()}]{IconSuccess} {text}[/]";
    public static string ErrorText(string text) => $"[{ErrorColor.ToMarkup()}]{IconError} {text}[/]";
    public static string WarningText(string text) => $"[{WarningColor.ToMarkup()}]{IconWarning} {text}[/]";
    public static string InfoText(string text) => $"[{InfoColor.ToMarkup()}]{IconInfo} {text}[/]";
    public static string PathText(string text) => $"[{PathColor.ToMarkup()}]{text}[/]";
    public static string MutedText(string text) => $"[{MutedColor.ToMarkup()}]{text}[/]";
    public static string AccentText(string text) => $"[{AccentColor.ToMarkup()}]{text}[/]";

    public static string ProfileStatusBar(PackageSmith.Core.Configuration.PackageSmithConfig config)
    {
        return $"[{MutedColor.ToMarkup()}]Profile: {config.CompanyName} | Unity: {config.DefaultUnityVersion} | License: MIT[/]";
    }
}

public static class ColorExtensions
{
    public static string ToMarkup(this Color color) => color.ToString().Replace("#", "").Replace(";", "").ToLower();
}
