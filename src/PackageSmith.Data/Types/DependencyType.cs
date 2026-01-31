using System;

namespace PackageSmith.Data.Types;

[Flags]
public enum DependencyType
{
	None = 0,
	UnityPackage = 1 << 0,
	NuGet = 1 << 1,
	Git = 1 << 2
}
