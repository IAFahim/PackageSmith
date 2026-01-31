using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using PackageSmith.Data.State;
using PackageSmith.Data.Types;

namespace PackageSmith.Core.Logic;

public static class AsmDefLogic
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void PlatformToString(AsmDefPlatform platform, out string platformStr)
	{
		platformStr = platform switch
		{
			AsmDefPlatform.Editor => "Editor",
			AsmDefPlatform.Windows => "Windows",
			AsmDefPlatform.Linux => "Linux",
			AsmDefPlatform.MacOS => "OSX",
			AsmDefPlatform.Android => "Android",
			AsmDefPlatform.iOS => "iOS",
			AsmDefPlatform.WebGL => "WebGL",
			_ => "AnyPlatform"
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GetStandardSubAssemblies(string packageName, out SubAssemblyState[] subAssemblies)
	{
		var baseName = packageName;
		var prefix = $"{baseName}.";

		subAssemblies = new SubAssemblyState[]
		{
			new() { Name = new FixedString64($"{prefix}Core"), Type = SubAssemblyType.Core, DependencyCount = 0 },
			new() { Name = new FixedString64($"{prefix}Data"), Type = SubAssemblyType.Data, DependencyCount = 1 },
			new() { Name = new FixedString64($"{prefix}Authoring"), Type = SubAssemblyType.Authoring, DependencyCount = 2 },
			new() { Name = new FixedString64($"{prefix}Runtime"), Type = SubAssemblyType.Runtime, DependencyCount = 2 },
			new() { Name = new FixedString64($"{prefix}Systems"), Type = SubAssemblyType.Systems, DependencyCount = 3 },
			new() { Name = new FixedString64($"{prefix}Debug"), Type = SubAssemblyType.Debug, DependencyCount = 3 }
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GetEcsReferences(in EcsPresetState preset, out ReferenceState[] references)
	{
		var list = new List<ReferenceState>();

		if (preset.EnableEntities) list.Add(new ReferenceState { Name = new FixedString64("Unity.Entities"), IsUnityReference = true });
		if (preset.EnableBurst) list.Add(new ReferenceState { Name = new FixedString64("Unity.Burst"), IsUnityReference = true });
		if (preset.EnableCollections) list.Add(new ReferenceState { Name = new FixedString64("Unity.Collections"), IsUnityReference = true });
		if (preset.EnableMathematics) list.Add(new ReferenceState { Name = new FixedString64("Unity.Mathematics"), IsUnityReference = true });
		if (preset.EnableJobs) list.Add(new ReferenceState { Name = new FixedString64("Unity.Jobs"), IsUnityReference = true });
		if (preset.EnablePhysics) list.Add(new ReferenceState { Name = new FixedString64("Unity.Physics"), IsUnityReference = true });
		if (preset.EnableEntitiesGraphics) list.Add(new ReferenceState { Name = new FixedString64("Unity.Entities.Graphics"), IsUnityReference = true });
		if (preset.EnableEntitiesHybrid) list.Add(new ReferenceState { Name = new FixedString64("Unity.Entities.Hybrid"), IsUnityReference = true });
		if (preset.EnableInputSystem) list.Add(new ReferenceState { Name = new FixedString64("Unity.InputSystem"), IsUnityReference = true });

		references = list.ToArray();
	}
}
