using System;

namespace PackageSmith.Data.Types;

[Flags]
public enum PackageModuleType
{
	None = 0,
	Runtime = 1 << 0,
	Editor = 1 << 1,
	Tests = 1 << 2,
	Samples = 1 << 3
}
