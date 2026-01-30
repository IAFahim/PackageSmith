namespace PackageSmith.Core.AssemblyDefinition;

[Flags]
public enum SubAssemblyType
{
    None = 0,
    Core = 1 << 0,
    Data = 1 << 1,
    Authoring = 1 << 2,
    Runtime = 1 << 3,
    Systems = 1 << 4,
    Debug = 1 << 5,

    Standard = Core | Data | Authoring | Runtime | Systems,
    WithDebug = Core | Data | Authoring | Runtime | Systems | Debug,
    All = Core | Data | Authoring | Runtime | Systems | Debug
}
