#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(ShowIfAttribute))]
public class ShowIfPropertyDrawer : PropertyDrawer
{
    VisualElement m_container;
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        ShowIfAttribute showIf = (ShowIfAttribute)attribute;
        m_container = new VisualElement();
        PropertyField propertyField = new (property);
        m_container.Add(propertyField);
        UpdateVisibility(property, showIf);

        // Track changes on the ENTIRE object
        m_container.TrackSerializedObjectValue(property.serializedObject, _ => 
        {
            UpdateVisibility(property, showIf);
        });

        return m_container;
    }

    void UpdateVisibility(SerializedProperty property, ShowIfAttribute foundAttribute)
    {
        if (m_container == null) return;
        bool conditionResult = GetConditionValue(property, foundAttribute.ConditionName);
        bool isVisible = foundAttribute.Invert ? !conditionResult : conditionResult;
        m_container.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        m_container.visible = isVisible;
    }

    static bool GetConditionValue(SerializedProperty property, string memberName)
    {
        object target = GetTargetObjectWithProperty(property);
        if (target == null)
        {
            UnityEngine.Debug.LogWarning($"ShowIf: Could not find target object for property '{property.name}'");
            return true;
        }

        Type type = target.GetType();
        MethodInfo method = type.GetMethod(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method != null && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
        {
            try
            {
                bool result = (bool)method.Invoke(target, null);
                return result;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"ShowIf: Error invoking method '{memberName}': {ex.Message}");
                return true;
            }
        }

        // B. Check for Property
        PropertyInfo prop = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.PropertyType == typeof(bool))
        {
            try
            {
                bool result = (bool)prop.GetValue(target);
                return result;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"ShowIf: Error getting property '{memberName}': {ex.Message}");
                return true;
            }
        }

        // C. Check for Field
        FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(bool))
        {
            try
            {
                bool result = (bool)field.GetValue(target);
                return result;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"ShowIf: Error getting field '{memberName}': {ex.Message}");
                return true;
            }
        }
        string propertyPath = property.propertyPath;
        int lastDot = propertyPath.LastIndexOf('.');
        string siblingPath = lastDot >= 0 ? $"{propertyPath[..lastDot]}.{memberName}" : memberName;
        SerializedProperty sibling = property.serializedObject.FindProperty(siblingPath);
        if (sibling is { propertyType: SerializedPropertyType.Boolean })
        {
            return sibling.boolValue;
        }
        
        // Try the auto-property backing field format
        string backingFieldName = $"<{memberName}>k__BackingField";
        string backingFieldPath = lastDot >= 0
            ? $"{propertyPath[..lastDot]}.{backingFieldName}"
            : backingFieldName;
            
        sibling = property.serializedObject.FindProperty(backingFieldPath);
        if (sibling is { propertyType: SerializedPropertyType.Boolean })
        {
            return sibling.boolValue;
        }

        // If nothing found, warn and default to visible
        UnityEngine.Debug.LogWarning($"ShowIf: Could not find condition '{memberName}' on type '{type.Name}' for property '{property.name}'");
        return true;
    }

    static object GetTargetObjectWithProperty(SerializedProperty property)
    {
        string path = property.propertyPath.Replace(".Array.data[", "[");
        object obj = property.serializedObject.targetObject;
        string[] elements = path.Split('.');

        foreach (string element in elements)
        {
            if (element == property.name) break;
            
            if (obj == null) return null;

            if (element.Contains("["))
            {
                string elementName = element[..element.IndexOf("[", StringComparison.Ordinal)];
                int index = Convert.ToInt32(element[element.IndexOf("[", StringComparison.Ordinal)..].Replace("[", "").Replace("]", ""));
                obj = GetValue_Imp(obj, elementName, index);
            }
            else
            {
                obj = GetValue_Imp(obj, element);
            }
        }
        return obj;
    }
    
    static object GetValue_Imp(object source, string name)
    {
        if (source == null) return null;
        Type type = source.GetType();
        while (type != null)
        {
            FieldInfo f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (f != null) return f.GetValue(source);
            PropertyInfo p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (p != null) return p.GetValue(source, null);

            type = type.BaseType;
        }
        return null;
    }

    static object GetValue_Imp(object source, string name, int index)
    {
        if (GetValue_Imp(source, name) is not IEnumerable enumerable) return null;
        IEnumerator enm = enumerable.GetEnumerator();
        using IDisposable disposable = enm as IDisposable;
        for (int i = 0; i <= index; i++)
        {
            if (!enm.MoveNext()) return null;
        }
        return enm.Current;
    }
}
#endif