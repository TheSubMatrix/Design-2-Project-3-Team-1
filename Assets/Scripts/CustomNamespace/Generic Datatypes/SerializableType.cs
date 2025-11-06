using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

[Serializable]
public class SerializableType : ISerializationCallbackReceiver {
    [FormerlySerializedAs("assemblyQualifiedName")] [SerializeField] string m_assemblyQualifiedName = string.Empty;
        
    public Type Type { get; private set; }
        
    void ISerializationCallbackReceiver.OnBeforeSerialize() {
        m_assemblyQualifiedName = Type?.AssemblyQualifiedName ?? m_assemblyQualifiedName;
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize() {
        if (!TryGetType(m_assemblyQualifiedName, out var type)) {
            Debug.LogError($"Type {m_assemblyQualifiedName} not found");
            return;
        }
        Type = type;
    }

    static bool TryGetType(string typeString, out Type type) {
        type = Type.GetType(typeString);
        return type != null || !string.IsNullOrEmpty(typeString);
    }
    
    public static implicit operator Type(SerializableType sType) => sType.Type;
    public static implicit operator SerializableType(Type type) => new() { Type = type };
}
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SerializableType))]
public class SerializableTypeDrawer : PropertyDrawer {
    TypeFilterAttribute m_typeFilter;
    string[] m_typeNames, m_typeFullNames;

    void Initialize() {
        if (m_typeFullNames != null) return;
            
        m_typeFilter = (TypeFilterAttribute) Attribute.GetCustomAttribute(fieldInfo, typeof(TypeFilterAttribute));
            
        Type[] filteredTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => m_typeFilter == null ? DefaultFilter(t) : m_typeFilter.Filter(t))
            .ToArray();
            
        m_typeNames = filteredTypes.Select(t => t.ReflectedType == null ? t.Name : $"{t.ReflectedType.Name}+{t.Name}").ToArray();
        m_typeFullNames = filteredTypes.Select(t => t.AssemblyQualifiedName).ToArray();
    }
        
    static bool DefaultFilter(Type type) {
        return !type.IsAbstract && !type.IsInterface && !type.IsGenericType;
    }

    public override VisualElement CreatePropertyGUI(SerializedProperty property) {
        Initialize();
        
        SerializedProperty typeIdProperty = property.FindPropertyRelative("assemblyQualifiedName");
        if (string.IsNullOrEmpty(typeIdProperty.stringValue)) {
            typeIdProperty.stringValue = m_typeFullNames.First();
            property.serializedObject.ApplyModifiedProperties();
        }

        int currentIndex = Array.IndexOf(m_typeFullNames, typeIdProperty.stringValue);
        if (currentIndex < 0) currentIndex = 0;

        PopupField<string> popupField = new PopupField<string>(
            property.displayName,
            m_typeNames.ToList(),
            currentIndex
        );

        // Handle value changes
        popupField.RegisterValueChangedCallback(evt => {
            int selectedIndex = m_typeNames.ToList().IndexOf(evt.newValue);
            if (selectedIndex < 0 || selectedIndex >= m_typeFullNames.Length) return;
            typeIdProperty.stringValue = m_typeFullNames[selectedIndex];
            property.serializedObject.ApplyModifiedProperties();
        });

        return popupField;
    }
}
#endif
public class TypeFilterAttribute : PropertyAttribute {
    public Func<Type, bool> Filter { get; }
        
    public TypeFilterAttribute(Type filterType) {
        Filter = type => !type.IsAbstract &&
                         !type.IsInterface &&
                         !type.IsGenericType &&
                         type.InheritsOrImplements(filterType);
    }
}