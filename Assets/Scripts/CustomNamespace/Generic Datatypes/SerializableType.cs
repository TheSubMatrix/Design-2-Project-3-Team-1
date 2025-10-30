using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

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
        
    // Implicit conversion from SerializableType to Type
    public static implicit operator Type(SerializableType sType) => sType.Type;

    // Implicit conversion from Type to SerializableType
    public static implicit operator SerializableType(Type type) => new() { Type = type };
}

[CustomPropertyDrawer(typeof(SerializableType))]
public class SerializableTypeDrawer : PropertyDrawer {
    TypeFilterAttribute typeFilter;
    string[] typeNames, typeFullNames;

    void Initialize() {
        if (typeFullNames != null) return;
            
        typeFilter = (TypeFilterAttribute) Attribute.GetCustomAttribute(fieldInfo, typeof(TypeFilterAttribute));
            
        Type[] filteredTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => typeFilter == null ? DefaultFilter(t) : typeFilter.Filter(t))
            .ToArray();
            
        typeNames = filteredTypes.Select(t => t.ReflectedType == null ? t.Name : $"t.ReflectedType.Name + t.Name").ToArray();
        typeFullNames = filteredTypes.Select(t => t.AssemblyQualifiedName).ToArray();
    }
        
    static bool DefaultFilter(Type type) {
        return !type.IsAbstract && !type.IsInterface && !type.IsGenericType;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        Initialize();
        SerializedProperty typeIdProperty = property.FindPropertyRelative("assemblyQualifiedName");

        if (string.IsNullOrEmpty(typeIdProperty.stringValue)) {
            typeIdProperty.stringValue = typeFullNames.First();
            property.serializedObject.ApplyModifiedProperties();
        }

        int currentIndex = Array.IndexOf(typeFullNames, typeIdProperty.stringValue);
        int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, typeNames);

        if (selectedIndex < 0 || selectedIndex == currentIndex) return;
        typeIdProperty.stringValue = typeFullNames[selectedIndex];
        property.serializedObject.ApplyModifiedProperties();
    }
}

public class TypeFilterAttribute : PropertyAttribute {
    public Func<Type, bool> Filter { get; }
        
    public TypeFilterAttribute(Type filterType) {
        Filter = type => !type.IsAbstract &&
                         !type.IsInterface &&
                         !type.IsGenericType &&
                         type.InheritsOrImplements(filterType);
    }
}

public static class TypeExtensions {
    /// <summary>
    /// Checks if a given type inherits or implements a specified base type.
    /// </summary>
    /// <param name="type">The type which needs to be checked.</param>
    /// <param name="baseType">The base type/interface which is expected to be inherited or implemented by the 'type'</param>
    /// <returns>Return true if 'type' inherits or implements 'baseType'. False otherwise</returns>        
    public static bool InheritsOrImplements(this Type type, Type baseType) {
        type = ResolveGenericType(type);
        baseType = ResolveGenericType(baseType);

        while (type != typeof(object)) {
            if (baseType == type || HasAnyInterfaces(type, baseType)) return true;
                
            type = ResolveGenericType(type.BaseType);
            if (type == null) return false;
        }
            
        return false;
    }
        
    static Type ResolveGenericType(Type type) {
        if (type is not { IsGenericType: true }) return type;

        Type genericType = type.GetGenericTypeDefinition();
        return genericType != type ? genericType : type;
    }

    static bool HasAnyInterfaces(Type type, Type interfaceType) {
        return type.GetInterfaces().Any(i => ResolveGenericType(i) == interfaceType);
    }
}