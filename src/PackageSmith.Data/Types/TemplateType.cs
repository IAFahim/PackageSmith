using System;

namespace PackageSmith.Data.Types;

[Flags]
public enum TemplateType
{
	None = 0,
	MonoBehaviour = 1 << 0,
	ScriptableObject = 1 << 1,
	EditorWindow = 1 << 2,
	SystemBase = 1 << 3,
	ISystem = 1 << 4,
	IComponentData = 1 << 5,
	ISharedComponentData = 1 << 6,
	Baker = 1 << 7,
	Authoring = 1 << 8,
	EcsFull = 1 << 9,
	DodFull = 1 << 10
}
