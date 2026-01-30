namespace PackageSmith.Core.Configuration;

[Flags]
public enum PackageModule
{
    None = 0,
    Runtime = 1 << 0,
    Editor = 1 << 1,
    Tests = 1 << 2,
    Samples = 1 << 3
}

public static class PackageModuleExtensions
{
    public static string ToFolderName(this PackageModule module) => module switch
    {
        PackageModule.Runtime => "Runtime",
        PackageModule.Editor => "Editor",
        PackageModule.Tests => "Tests",
        PackageModule.Samples => "Samples",
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, null)
    };

    public static PackageModule[] AllValues => new[]
    {
        PackageModule.Runtime,
        PackageModule.Editor,
        PackageModule.Tests,
        PackageModule.Samples
    };
}
