using System.Runtime.CompilerServices;

namespace PackageSmith.Core.Logic;

public static class TemplateLogic
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GenerateMonoBehaviour(string ns, string className)
	{
		return $$"""
		using UnityEngine;

		namespace {{ns}}
		{
			public class {{className}} : MonoBehaviour
			{
				private void Start()
				{
				}
			}
		}
		""";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GenerateScriptableObject(string ns, string className)
	{
		return $$"""
		using UnityEngine;

		namespace {{ns}}
		{
			[CreateAssetMenu(fileName = "{{className}}", menuName = "Data/{{className}}")]
			public class {{className}} : ScriptableObject
			{
			}
		}
		""";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GenerateIComponentData(string ns, string componentName)
	{
		return $$"""
		using Unity.Entities;

		namespace {{ns}}
		{
			public struct {{componentName}} : IComponentData
			{
			}
		}
		""";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GenerateSystemBase(string ns, string systemName)
	{
		return $$"""
		using Unity.Entities;
		using Unity.Burst;

		namespace {{ns}}
		{
			[BurstCompile]
			public partial struct {{systemName}} : ISystem
			{
				[BurstCompile]
				public void OnCreate(ref SystemState state)
				{
				}

				[BurstCompile]
				public void OnUpdate(ref SystemState state)
				{
				}

				[BurstCompile]
				public void OnDestroy(ref SystemState state)
				{
				}
			}
		}
		""";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GenerateAuthoring(string ns, string featureName, string componentName)
	{
		return $$"""
		using UnityEngine;
		using Unity.Entities;
		using Unity.Mathematics;

		namespace {{ns}}
		{
			[DisallowMultipleComponent]
			public class {{featureName}}Authoring : MonoBehaviour
			{
				[field: SerializeField] public float Value { get; set; } = 1.0f;

				private class Baker : Baker<{{featureName}}Authoring>
				{
					public override void Bake({{featureName}}Authoring authoring)
					{
						var entity = GetEntity(TransformUsageFlags.Dynamic);
						AddComponent(entity, new {{componentName}}
						{
							Value = authoring.Value
						});
					}
				}
			}
		}
		""";
	}
}
