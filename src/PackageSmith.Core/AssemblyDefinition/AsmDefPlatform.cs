namespace PackageSmith.Core.AssemblyDefinition;

[Flags]
public enum AsmDefPlatform
{
    None = 0,
    Editor = 1,
    Windows = 2,
    Linux = 4,
    MacOS = 8,
    Android = 16,
    iOS = 32,
    WebGL = 64,
    All = Editor | Windows | Linux | MacOS | Android | iOS | WebGL
}
