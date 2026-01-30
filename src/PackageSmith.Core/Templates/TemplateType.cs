namespace PackageSmith.Core.Templates;

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
    Authoring = IComponentData | Baker | MonoBehaviour,

    Standard = MonoBehaviour | ScriptableObject,
    EcsStandard = SystemBase | IComponentData | Authoring,
    EcsFull = SystemBase | ISystem | IComponentData | ISharedComponentData | Baker | Authoring
}
