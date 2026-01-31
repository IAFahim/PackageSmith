using System;

namespace PackageSmith.Data.Types;

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
	WebGL = 64
}
