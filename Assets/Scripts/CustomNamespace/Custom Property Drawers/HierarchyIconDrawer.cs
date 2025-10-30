using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class HierarchyIconDrawer
{
    static readonly Texture2D s_RequiredIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Scripts/CustomNamespace/Custom Property Drawers/Drawer Assets/RequiredFieldIcon.png");
    static readonly Dictionary<Type, FieldInfo[]> s_CachedRequiredFields = new();
    static readonly Dictionary<Type, FieldInfo[]> s_CachedSerializedFields = new();
    private static readonly Type s_RequiredFieldAttributeType = typeof(RequiredFieldAttribute);
    private static readonly Type s_SerializeFieldAttributeType = typeof(SerializeField);

    static HierarchyIconDrawer()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemOnGUI;
    }

    static void OnHierarchyItemOnGUI(int instanceID, Rect selectionRect)
    {
        if (EditorUtility.InstanceIDToObject(instanceID) is not GameObject gameObject) return;
        if (!gameObject.GetComponents<Component>()
                .Where(component => component is not null)
                .Any(component => HasUnassignedRequiredField(component, component.GetType(), component.GetType()))) return;
        Rect iconRect = new Rect(selectionRect.xMax - 20, selectionRect.y, 16, 16);
        GUI.Label(iconRect, new GUIContent(s_RequiredIcon, "One or more required fields are missing or empty."));
    }
    static bool HasUnassignedRequiredField(object targetObject, Type fieldType, Type rootComponentType)
    {
        if (targetObject == null)
        {
            return false;
        }
        FieldInfo[] requiredFields = GetCachedRequiredFields(fieldType);
        if (requiredFields.Any(field => IsFieldUnassigned(field.GetValue(targetObject))))
        {
            return true;
        }
        FieldInfo[] serializedFields = GetCachedSerializedFields(fieldType);
        return (from field in serializedFields let value = field.GetValue(targetObject) let valueType = field.FieldType 
            where valueType != rootComponentType 
            where value != null && valueType.IsClass && !valueType.IsArray && !valueType.IsPrimitive && !valueType.IsSubclassOf(typeof(UnityEngine.Object)) && valueType != typeof(string) 
            where HasUnassignedRequiredField(value, valueType, rootComponentType) select value).Any();
    }
    static bool IsFieldUnassigned(object fieldValue)
    {
        return fieldValue switch
        {
            null => true,
            string value => string.IsNullOrEmpty(value),
            UnityEngine.Object unityObj => unityObj == null,
            IEnumerable enumerable => IsEnumerableEmpty(enumerable),
            _ => CheckForCustomContainerEmptiness(fieldValue)
        };
    }
    static bool CheckForCustomContainerEmptiness(object fieldValue)
    {
        if (fieldValue == null) return false;
        Type fieldType = fieldValue.GetType();
        PropertyInfo countProperty = fieldType.GetProperty("Count");
        if (countProperty != null && countProperty.PropertyType == typeof(int))
        {
            int count = (int)countProperty.GetValue(fieldValue, null);
            return count == 0;
        }
        PropertyInfo lengthProperty = fieldType.GetProperty("Length");
        if (lengthProperty != null && lengthProperty.PropertyType == typeof(int))
        {
            int length = (int)lengthProperty.GetValue(fieldValue, null);
            return length == 0;
        }
        PropertyInfo isEmptyProperty = fieldType.GetProperty("IsEmpty");
        if (isEmptyProperty == null || isEmptyProperty.PropertyType != typeof(bool)) return false;
        bool isEmpty = (bool)isEmptyProperty.GetValue(fieldValue, null);
        return isEmpty;
    }

    static bool IsEnumerableEmpty(IEnumerable enumerable)
    {
        IEnumerator enumerator = enumerable.GetEnumerator();
        try
        {
            return !enumerator.MoveNext();
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    /// <summary>Caches and returns fields marked with RequiredFieldAttribute.</summary>
    static FieldInfo[] GetCachedRequiredFields(Type componentType)
    {
        if (s_CachedRequiredFields.TryGetValue(componentType, out FieldInfo[] fields)) return fields;

        FieldInfo[] allFields = componentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        // A field is considered 'required' if it is both serialized AND marked with RequiredFieldAttribute.
        FieldInfo[] requiredFields = allFields
            .Where(field => (field.IsPublic || field.IsDefined(s_SerializeFieldAttributeType)) && field.IsDefined(s_RequiredFieldAttributeType, false))
            .ToArray();
            
        s_CachedRequiredFields[componentType] = requiredFields;
        return requiredFields;
    }

    /// <summary>Caches and returns all fields that Unity serializes.</summary>
    static FieldInfo[] GetCachedSerializedFields(Type componentType)
    {
        if (s_CachedSerializedFields.TryGetValue(componentType, out FieldInfo[] fields)) return fields;

        FieldInfo[] allFields = componentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        // A field is serialized if it is public OR marked with [SerializeField]
        FieldInfo[] serializedFields = allFields
            .Where(field => field.IsPublic || field.IsDefined(s_SerializeFieldAttributeType))
            .ToArray();

        s_CachedSerializedFields[componentType] = serializedFields;
        return serializedFields;
    }
}