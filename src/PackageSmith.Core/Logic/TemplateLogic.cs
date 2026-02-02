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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GenerateDodFull(string ns, string featureName)
	{
		return $$"""
		using System;
		using System.Runtime.CompilerServices;
		using System.Runtime.InteropServices;
		using UnityEngine;

		namespace {{ns}}
		{
			// LAYER A: DATA
			[Serializable]
			[StructLayout(LayoutKind.Sequential)]
			public struct {{featureName}}State
			{
				public float Value;
				public bool IsActive;

				public override string ToString() => $"[{{featureName}}] {Value:F1} ({(IsActive ? "ON" : "OFF")})"; // Debug view
			}

			// LAYER B: LOGIC
			public static class {{featureName}}Logic
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				public static void Calculate(float current, float mod, out float result)
				{
					result = current + mod; // Atomic operation
				}
			}

			// LAYER C: EXTENSIONS
			public static class {{featureName}}Extensions
			{
				public static bool TryModify(ref this {{featureName}}State state, float amount, out float result)
				{
					result = state.Value; // Default

					if (!state.IsActive) return false; // Guard
					if (amount == 0) return false; // Guard

					{{featureName}}Logic.Calculate(state.Value, amount, out result); // Execute
					state.Value = result; // Apply

					return true; // Success
				}
			}

			// LAYER D: INTERFACE & BRIDGE
			public interface I{{featureName}}System
			{
				bool TryModify(float amount);
			}

			public class {{featureName}}Component : MonoBehaviour, I{{featureName}}System
			{
				[SerializeField] private {{featureName}}State _state;

				bool I{{featureName}}System.TryModify(float amount)
				{
					return _state.TryModify(amount, out _); // Proxy
				}

				[ContextMenu("Debug: Test Modify")]
				private void TestModify()
				{
					var success = ((I{{featureName}}System)this).TryModify(10f); // Explicit call
					Debug.Log(success ? $"<color=cyan>OK:</color> {_state}" : "FAIL"); // Log
				}
			}
		}
		""";
	}
}
