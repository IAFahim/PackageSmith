using System;
using System.Runtime.InteropServices;
using PackageSmith.Data.Types;

namespace PackageSmith.Data.State;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct PackageState
{
	public FixedString64 PackageName;
	public FixedString64 DisplayName;
	public FixedString64 Description;
	public PackageModuleType SelectedModules;
	public FixedString64 OutputPath;
	public FixedString64 CompanyName;
	public FixedString64 UnityVersion;
	public EcsPresetState EcsPreset;
	public SubAssemblyType SubAssemblies;
	public bool EnableSubAssemblies;
	public int DependencyCount;
	public TemplateType SelectedTemplate;

	public override readonly string ToString() => $"[Package] {PackageName.ToString()} ({DisplayName.ToString()})";
}

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct EcsPresetState
{
	public bool EnableEntities;
	public bool EnableBurst;
	public bool EnableCollections;
	public bool EnableMathematics;
	public bool EnableJobs;
	public bool EnablePhysics;
	public bool EnableEntitiesGraphics;
	public bool EnableEntitiesHybrid;
	public bool EnableInputSystem;

	public override readonly string ToString() => $"[ECS] {(EnableEntities ? "ON" : "OFF")}";
}
