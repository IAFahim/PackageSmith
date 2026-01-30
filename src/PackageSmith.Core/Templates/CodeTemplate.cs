namespace PackageSmith.Core.Templates;

public static class CodeTemplate
{
    public static string MonoBehaviour(string ns, string className, string? description = null)
    {
        var desc = string.IsNullOrWhiteSpace(description) ? $"Add {className} description here" : description;
        return $$"""
        using UnityEngine;

        namespace {{ns}};

        /// <summary>{{desc}}</summary>
        public class {{className}} : MonoBehaviour
        {
            [SerializeField]
            private float _value = 1f;

            private void Awake()
            {
                // Initialize
            }

            private void Update()
            {
                // Update logic
            }
        }
        """;
    }

    public static string ScriptableObject(string ns, string className, string? description = null)
    {
        var desc = string.IsNullOrWhiteSpace(description) ? $"Add {className} description here" : description;
        return $$"""
        using UnityEngine;

        namespace {{ns}};

        /// <summary>{{desc}}</summary>
        [CreateAssetMenu(fileName = "{{className}}", menuName = "Data/{{className}}")]
        public class {{className}} : ScriptableObject
        {
            [SerializeField]
            private float _configValue = 1f;

            public float ConfigValue => _configValue;

            private void OnEnable()
            {
                // Initialization
            }
        }
        """;
    }

    public static string SystemBase(string ns, string className, string? description = null)
    {
        var desc = string.IsNullOrWhiteSpace(description) ? $"Add {className} description here" : description;
        return $$"""
        using Unity.Entities;
        using Unity.Burst;

        namespace {{ns}};

        /// <summary>{{desc}}</summary>>
        [BurstCompile]
        public partial struct {{className}}System : ISystem
        {
            [BurstCompile]
            public void OnCreate(ref SystemState state)
            {
                // Initialization
            }

            [BurstCompile]
            public void OnDestroy(ref SystemState state)
            {
                // Cleanup
            }

            [BurstCompile]
            public void OnUpdate(ref SystemState state)
            {
                // System logic
            }
        }
        """;
    }

    public static string ISystem(string ns, string className, string? description = null)
    {
        var desc = string.IsNullOrWhiteSpace(description) ? $"Add {className} description here" : description;
        return $$"""
        using Unity.Entities;
        using Unity.Burst;

        namespace {{ns}};

        /// <summary>{{desc}}</summary>
        [BurstCompile]
        public partial struct {{className}} : ISystemEntity
        {
            // Entity-based system logic
        }
        """;
    }

    public static string IComponentData(string ns, string className, string? description = null)
    {
        var desc = string.IsNullOrWhiteSpace(description) ? $"Add {className} description here" : description;
        return $$"""
        using Unity.Entities;

        namespace {{ns}};

        /// <summary>{{desc}}</summary>
        public struct {{className}} : IComponentData
        {
            public float Value;
            public int Count;
        }
        """;
    }

    public static string ISharedComponentData(string ns, string className, string? description = null)
    {
        var desc = string.IsNullOrWhiteSpace(description) ? $"Add {className} description here" : description;
        return $$"""
        using Unity.Entities;

        namespace {{ns}};

        /// <summary>{{desc}}</summary>
        public struct {{className}} : ISharedComponentData, IComponentData, IEquatable<{{className}}>
        {
            public bool IsActive;

            public bool Equals({{className}} other)
            {
                return IsActive == other.IsActive;
            }

            public override int GetHashCode()
            {
                return IsActive.GetHashCode();
            }
        }
        """;
    }

    public static string Baker(string ns, string authoringClassName, string componentDataClassName, string? description = null)
    {
        var desc = string.IsNullOrWhiteSpace(description) ? $"Add {authoringClassName} description here" : description;
        return $$"""
        using Unity.Entities;
        using Unity.Mathematics;

        namespace {{ns}};

        /// <summary>{{desc}}</summary>
        public class {{authoringClassName}}Baker : Baker<{{authoringClassName}}>
        {
            public override void Bake({{authoringClassName}} authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new {{componentDataClassName}}
                {
                    Value = authoring.Value,
                    Count = authoring.Count
                });
            }
        }
        """;
    }

    public static string AuthoringComponent(string ns, string className, string? description = null)
    {
        var desc = string.IsNullOrWhiteSpace(description) ? $"Add {className} description here" : description;
        return $$"""
        using Unity.Entities;
        using UnityEngine;

        namespace {{ns}};

        /// <summary>{{desc}}</summary>
        public class {{className}} : MonoBehaviour
        {
            [SerializeField]
            private float _value = 1f;

            [SerializeField]
            private int _count = 0;

            public float Value => _value;
            public int Count => _count;
        }
        """;
    }

    public static string ScaffoldAuthoring(string ns, string featureName, string componentDataName)
    {
        var authoringName = $"{featureName}Authoring";

        return $$"""
        using Unity.Entities;
        using UnityEngine;

        namespace {{ns}};

        // ==========================================
        // COMPONENT DATA
        // ==========================================

        public struct {{componentDataName}} : IComponentData
        {
            public float Value;
            public int Count;
        }

        // ==========================================
        // AUTHOURING COMPONENT
        // ==========================================

        public class {{authoringName}} : MonoBehaviour
        {
            [SerializeField]
            private float _value = 1f;

            [SerializeField]
            private int _count = 0;

            public float Value => _value;
            public int Count => _count;
        }

        // ==========================================
        // BAKER
        // ==========================================

        public class {{authoringName}}Baker : Baker<{{authoringName}}>
        {
            public override void Bake({{authoringName}} authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new {{componentDataName}}
                {
                    Value = authoring.Value,
                    Count = authoring.Count
                });
            }
        }
        """;
    }

    public static string TestRunner(string ns, string className)
    {
        return $$"""
        using NUnit.Framework;
        using Unity.Entities;
        using UnityEngine;

        namespace {{ns}};

        public class {{className}}
        {
            [SetUp]
            public void SetUp()
            {
                // Setup test environment
            }

            [TearDown]
            public void TearDown()
            {
                // Cleanup test environment
            }

            [Test]
            public void SampleTest_Passes()
            {
                // Arrange
                var expected = 1;

                // Act
                var actual = 1;

                // Assert
                Assert.AreEqual(expected, actual);
            }
        }
        """;
    }
}
