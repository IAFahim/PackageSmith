using Spectre.Console;

namespace PackageSmith.UI;

public static class StyleManager
{
    // --- Professional Color Palette (Teal/Slate/Green) ---

    // Primary: User input, focus, interactive elements
    public static readonly Color Primary = new Color(30, 160, 160);    // Teal/Cyan

    // Secondary: Content, text body
    public static readonly Color Secondary = new Color(220, 220, 220); // Off-White
    public static readonly Color Content = Secondary; // Alias for compatibility

    // Tertiary: Metadata, paths, help text
    public static readonly Color Tertiary = new Color(100, 100, 100);  // Dim Grey
    public static readonly Color Dim = Tertiary; // Alias for compatibility

    // Status colors (minimal usage)
    public static readonly Color SuccessColor = new Color(50, 200, 100);
    public static readonly Color ErrorColor = new Color(200, 50, 50);
    public static readonly Color WarningColor = new Color(220, 180, 0);
    public static readonly Color InfoColor = new Color(100, 150, 220);

    // Semantic colors (from old palette, kept for compatibility)
    public static readonly Color CommandColor = Primary;
    public static readonly Color PathColor = InfoColor;
    public static readonly Color MutedColor = Tertiary;
    public static readonly Color AccentColor = Primary;

    // --- Technical Symbols (ASCII/Unicode > Emojis) ---
    public const string SymTick = "\u2713";    // ✓
    public const string SymSuccess = SymTick; // Alias for compatibility
    public const string SymCross = "\u00D7";  // ×
    public const string SymError = SymCross;  // Alias for compatibility
    public const string SymArrow = "\u2192";  // →
    public const string SymBullet = "\u2022"; // •
    public const string SymInfo = "\u203A";   // ›
    public const string SymWarning = "!";     // !

    // Legacy emoji icons (mapped to symbols for compatibility)
    public const string IconPackage = SymBullet;
    public const string IconConfig = SymBullet;
    public const string IconDeploy = SymBullet;
    public const string IconCode = SymBullet;
    public const string IconFolder = SymBullet;
    public const string IconSuccess = SymTick;
    public const string IconError = SymCross;
    public const string IconWarning = SymWarning;
    public const string IconInfo = SymInfo;
    public const string IconGit = SymBullet;
    public const string IconTemplate = SymBullet;
    public const string IconDependency = SymBullet;
    public const string IconTest = SymBullet;

    // Token System - Strict semantic styles
    public static Style Label => new Style(Tertiary);
    public static Style Value => new Style(Secondary);
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

    // --- Markup Helpers ---
    public static string PrimaryText(string text) => $"[{Primary.ToMarkup()}]{text}[/]";
    public static string SecondaryText(string text) => $"[{Secondary.ToMarkup()}]{text}[/]";
    public static string TertiaryText(string text) => $"[{Tertiary.ToMarkup()}]{text}[/]";
    public static string SuccessText(string text) => $"[{SuccessColor.ToMarkup()}]{SymTick} {text}[/]";
    public static string ErrorText(string text) => $"[{ErrorColor.ToMarkup()}]{SymCross} {text}[/]";
    public static string WarningText(string text) => $"[{WarningColor.ToMarkup()}]{SymWarning} {text}[/]";
    public static string InfoText(string text) => $"[{InfoColor.ToMarkup()}]{SymInfo} {text}[/]";
    public static string PathText(string text) => $"[{PathColor.ToMarkup()}]{text}[/]";
    public static string MutedText(string text) => $"[{Tertiary.ToMarkup()}]{text}[/]";
    public static string AccentText(string text) => $"[{Primary.ToMarkup()}]{text}[/]";

    // Legacy helpers (for compatibility)
    public static string Package(string text) => AccentText(text);

    public static string ProfileStatusBar(PackageSmith.Core.Configuration.PackageSmithConfig config)
    {
        return $"[{Tertiary.ToMarkup()}]Profile: {config.CompanyName} | Unity: {config.DefaultUnityVersion}[/]";
    }

    // Tree symbols for file visualization
    public static string TreeBranch => "├──";
    public static string TreeEnd => "└──";
    public static string TreeVertical => "│  ";
    public static string TreeVert => TreeVertical; // Alias for compatibility
}

public static class ColorExtensions
{
    public static string ToMarkup(this Color color) => color.ToString().Replace("#", "").Replace(";", "").ToLower();
}
