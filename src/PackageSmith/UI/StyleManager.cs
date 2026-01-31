using Spectre.Console;

namespace PackageSmith.UI;

public static class StyleManager
{
    // Professional Color Palette - "Minimalist Technical"

    // Primary: User input, focus, interactive elements
    public static readonly Color Primary = new Color(0, 200, 200); // Cyan

    // Secondary: Content, text body
    public static readonly Color Content = Color.White;

    // Tertiary: Metadata, paths, help text
    public static readonly Color Dim = new Color(100, 100, 100);

    // Status colors (minimal usage) - use *Color suffix to avoid conflict with Style properties
    public static readonly Color SuccessColor = new Color(0, 200, 100);  // Green
    public static readonly Color ErrorColor = new Color(220, 50, 50);     // Red
    public static readonly Color WarningColor = new Color(220, 160, 0);  // Yellow/Orange
    public static readonly Color InfoColor = new Color(100, 150, 220);   // Soft Blue

    // Semantic colors (from old palette, kept for compatibility)
    public static readonly Color CommandColor = Primary;
    public static readonly Color PathColor = InfoColor;
    public static readonly Color MutedColor = Dim;
    public static readonly Color AccentColor = Primary;

    // Technical Symbols (replacing emojis)
    public const string SymSuccess = "\u2713";  // ✓
    public const string SymError = "\u00D7";    // ×
    public const string SymInfo = "\u203A";      // ›
    public const string SymWarning = "!";        // !
    public const string SymBullet = "\u2022";    // •
    public const string SymArrow = "\u2192";     // →

    // Legacy emoji icons (deprecated, use symbols above)
    public const string IconPackage = "";
    public const string IconConfig = "";
    public const string IconDeploy = "";
    public const string IconCode = "";
    public const string IconFolder = "";
    public const string IconSuccess = SymSuccess;
    public const string IconError = SymError;
    public const string IconWarning = SymWarning;
    public const string IconInfo = SymInfo;
    public const string IconGit = "";
    public const string IconTemplate = "";
    public const string IconDependency = "";
    public const string IconTest = "";

    // Token System - Strict semantic styles
    public static Style Label => new Style(Dim);
    public static Style Value => new Style(Content);
    public static Style Highlight => new Style(Primary);
    public static Style StatusSuccess => new Style(SuccessColor);
    public static Style StatusError => new Style(ErrorColor);
    public static Style StatusWarning => new Style(WarningColor);
    public static Style StatusInfo => new Style(InfoColor);

    // Legacy styles (for compatibility)
    public static Style Command => new Style(CommandColor);
    public static Style Warning => new Style(WarningColor);
    public static Style Error => new Style(ErrorColor);
    public static Style Success => new Style(SuccessColor);
    public static Style Path => new Style(PathColor);
    public static Style InfoStyle => new Style(InfoColor);
    public static Style Muted => new Style(MutedColor);
    public static Style Accent => new Style(AccentColor);

    // Formatted markup helpers (using new symbols)
    public static string SuccessText(string text) => $"[{SuccessColor.ToMarkup()}]{SymSuccess} {text}[/]";
    public static string ErrorText(string text) => $"[{ErrorColor.ToMarkup()}]{SymError} {text}[/]";
    public static string WarningText(string text) => $"[{WarningColor.ToMarkup()}]{SymWarning} {text}[/]";
    public static string InfoText(string text) => $"[{InfoColor.ToMarkup()}]{SymInfo} {text}[/]";
    public static string PathText(string text) => $"[{PathColor.ToMarkup()}]{text}[/]";
    public static string MutedText(string text) => $"[{Dim.ToMarkup()}]{text}[/]";
    public static string AccentText(string text) => $"[{Primary.ToMarkup()}]{text}[/]";

    // Legacy helpers (for compatibility)
    public static string Package(string text) => AccentText(text);

    public static string ProfileStatusBar(PackageSmith.Core.Configuration.PackageSmithConfig config)
    {
        return $"[{Dim.ToMarkup()}]{config.CompanyName} | Unity {config.DefaultUnityVersion} | MIT[/]";
    }

    // Tree symbols for file visualization
    public static string TreeBranch => "├──";
    public static string TreeEnd => "└──";
    public static string TreeVertical => "│  ";
}

public static class ColorExtensions
{
    public static string ToMarkup(this Color color) => color.ToString().Replace("#", "").Replace(";", "").ToLower();
}
