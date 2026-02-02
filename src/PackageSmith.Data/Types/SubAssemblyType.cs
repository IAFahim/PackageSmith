using System;

namespace PackageSmith.Data.Types;

[Flags]
public enum SubAssemblyType
{
    None = 0,
    Core = 1 << 0,
    Data = 1 << 1,
    Authoring = 1 << 2,
    Runtime = 1 << 3,
    Systems = 1 << 4,
    Editor = 1 << 5,
    Tests = 1 << 6,
    Debug = 1 << 7
}