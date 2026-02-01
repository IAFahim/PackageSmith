using System.Collections.Generic;
using System.Runtime.CompilerServices;
using PackageSmith.Data.State;

namespace PackageSmith.Core.Logic;

public static class DependencyLogic
{
	private static readonly Dictionary<string, string[]> Shortcuts = new()
	{
		["ecs"] = new[] { "com.unity.entities", "com.unity.burst", "com.unity.collections", "com.unity.jobs" },
		["2d"] = new[] { "com.unity.2d.sprite", "com.unity.2d.tilemap" },
		["urp"] = new[] { "com.unity.render-pipelines.universal" },
		["hdrp"] = new[] { "com.unity.render-pipelines.high-definition" },
		["netcode"] = new[] { "com.unity.netcode.gameobjects", "com.unity.transport" }
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ResolveDependencies(string[] inputs, out PackageDependency[] results)
	{
		var list = new List<PackageDependency>();

		foreach (var input in inputs)
		{
			var lower = input.ToLowerInvariant();
			if (Shortcuts.TryGetValue(lower, out var expanded))
			{
				foreach (var item in expanded)
				{
					list.Add(new PackageDependency(item, "latest"));
				}
			}
			else
			{
				if (lower.Contains("."))
				{
					list.Add(new PackageDependency(lower, "latest"));
				}
			}
		}

		results = list.ToArray();
	}
}
