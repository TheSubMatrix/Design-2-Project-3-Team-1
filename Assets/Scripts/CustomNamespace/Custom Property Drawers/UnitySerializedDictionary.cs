using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif

[Serializable]
public struct SerializableKeyValuePair<TKey, TValue>
{
    public TKey Key;
    public TValue Value;

    public SerializableKeyValuePair(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }
}

[Serializable]
public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver, IEnumerable
{
    [FormerlySerializedAs("list")] [SerializeField]
     List<SerializableKeyValuePair<TKey, TValue>> m_list = new();
    [SerializeField]
    SerializableKeyValuePair<TKey, TValue> m_stagingEntry;

    [NonSerialized]
    Dictionary<TKey, TValue> m_dictionary;
    
    [NonSerialized]
    bool m_initialized;
    
    public Dictionary<TKey, TValue> Dictionary 
    { 
        get 
        {
            EnsureInitialized();
            return m_dictionary;
        }
    }

    void EnsureInitialized()
    {
        if (m_initialized && m_dictionary != null) return;
        
        if (m_dictionary == null)
            m_dictionary = new Dictionary<TKey, TValue>();
        else
            m_dictionary.Clear();
        foreach (SerializableKeyValuePair<TKey, TValue> kvp in m_list.Where(kvp => kvp.Key != null))
        {
            m_dictionary[kvp.Key] = kvp.Value;
        }
        
        m_initialized = true;
    }

    public TValue this[TKey key]
    {
        get => Dictionary[key];
        set 
        { 
            Dictionary[key] = value;
            m_initialized = true;
        }
    }

    public void Add(TKey key, TValue value) 
    { 
        Dictionary.Add(key, value);
        m_initialized = true;
    }
    
    public bool Remove(TKey key) 
    { 
        bool result = Dictionary.Remove(key);
        if (result) m_initialized = true;
        return result;
    }
    
    public bool ContainsKey(TKey key) => Dictionary.ContainsKey(key);
    public bool TryGetValue(TKey key, out TValue value) => Dictionary.TryGetValue(key, out value);
    
    public void Clear() 
    { 
        Dictionary.Clear();
        m_initialized = true;
    }
    
    public int Count => Dictionary.Count;

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Dictionary.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Dictionary.GetEnumerator();

    public void OnBeforeSerialize()
    {
        if (!m_initialized || m_dictionary is not { Count: > 0 }) return;
        m_list.Clear();
        foreach (KeyValuePair<TKey, TValue> kvp in m_dictionary)
        {
            m_list.Add(new SerializableKeyValuePair<TKey, TValue>(kvp.Key, kvp.Value));
        }
    }

    public void OnAfterDeserialize()
    {
        m_initialized = false;
    }
    
    public static implicit operator Dictionary<TKey, TValue>(SerializableDictionary<TKey, TValue> dictionary)
    {
        return dictionary.Dictionary;
    }
    
    public static implicit operator SerializableDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
    {
        return new SerializableDictionary<TKey, TValue> { m_dictionary = new Dictionary<TKey, TValue>(dictionary), m_initialized = true }; 
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
public class SerializableDictionaryDrawer : PropertyDrawer
{
    static readonly Dictionary<Type, BuildUIForType> s_UIBuilderCache = new();
    delegate void BuildUIForType(SerializedProperty property, VisualElement container);
    
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        Type[] genericArgs = fieldInfo.FieldType.GetGenericArguments();
        
        if (genericArgs.Length != 2)
        {
            return new Label("Error: Invalid SerializableDictionary type");
        }
        Type keyType = genericArgs[0];
        Type valueType = genericArgs[1];
        
        VisualElement container = new ();
        SerializedProperty listProperty = property.FindPropertyRelative("m_list");
        SerializedProperty stagingProperty = property.FindPropertyRelative("m_stagingEntry");
        Foldout foldout = new()
        {
            text = $"{property.displayName} ({listProperty.arraySize} entries)",
            value = property.isExpanded
        };
        foldout.RegisterValueChangedCallback(evt => property.isExpanded = evt.newValue);
        VisualElement listContainer = new ();
        
        VisualElement newEntrySection = new()
        {
            style = 
            { 
                backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 0.2f)),
                paddingTop = 5,
                paddingBottom = 5,
                paddingLeft = 5,
                paddingRight = 5,
                marginBottom = new StyleLength(5),
                borderBottomWidth = 1,
                borderBottomColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 0.5f))
            }
        };

        Label newEntryLabel = new("Add/Update Entry")
        {
            style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 }
        };
        newEntrySection.Add(newEntryLabel);
        SerializedProperty stagingKey = stagingProperty.FindPropertyRelative("Key");
        SerializedProperty stagingValue = stagingProperty.FindPropertyRelative("Value");

        VisualElement stagingRow = new()
        {
            style = { flexDirection = FlexDirection.Column, marginBottom = 2 }
        };
        VisualElement keyContainer = new() { style = { marginBottom = 2 } };
        DrawUIForType(keyType, stagingKey, keyContainer);
        VisualElement valueContainer = new() { style = { marginBottom = 5 } };
        DrawUIForType(valueType, stagingValue, valueContainer);

        Button addButton = new(() =>
        {
            int existingIndex = -1;
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                SerializedProperty existingKey = element.FindPropertyRelative("Key");

                if (!SerializedProperty.DataEquals(existingKey, stagingKey)) continue;
                existingIndex = i;
                break;
            }
            
            SerializedProperty targetElement;
            
            if (existingIndex >= 0)
            {
                targetElement = listProperty.GetArrayElementAtIndex(existingIndex);
            }
            else
            {
                int newIndex = listProperty.arraySize;
                listProperty.arraySize++;
                property.serializedObject.ApplyModifiedProperties();
                property.serializedObject.Update();
                targetElement = listProperty.GetArrayElementAtIndex(newIndex);
            }
            SerializedProperty targetKey = targetElement.FindPropertyRelative("Key");
            SerializedProperty targetValue = targetElement.FindPropertyRelative("Value");
            
            CopyPropertyValue(stagingKey, targetKey);
            CopyPropertyValue(stagingValue, targetValue);
            
            property.serializedObject.ApplyModifiedProperties();
            ClearPropertyValue(stagingKey, keyType);
            ClearPropertyValue(stagingValue, valueType);
            
            property.serializedObject.ApplyModifiedProperties();
        })
        {
            text = "Add/Update",
            style = { alignSelf = Align.FlexEnd, width = 80 }
        };

        stagingRow.Add(keyContainer);
        stagingRow.Add(valueContainer);
        stagingRow.Add(addButton);
        newEntrySection.Add(stagingRow);
        RebuildList();

        foldout.Add(newEntrySection);
        foldout.Add(listContainer);
        container.Add(foldout);
        container.TrackPropertyValue(listProperty, prop => 
        {
            foldout.text = $"{property.displayName} ({prop.arraySize} entries)";
            RebuildList(); 
        });

        return container;

        void RebuildList()
        {
            listContainer.Clear();
            
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                int index = i;
                SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                SerializedProperty keyProp = element.FindPropertyRelative("Key");
                SerializedProperty valueProp = element.FindPropertyRelative("Value");

                VisualElement entryRow = new()
                {
                    style = 
                    { 
                        flexDirection = FlexDirection.Column, 
                        marginBottom = 5,
                        paddingTop = 5,
                        paddingBottom = 5,
                        paddingLeft = 5,
                        paddingRight = 5,
                        
                        borderBottomWidth = 1,
                        borderBottomColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 0.5f))
                    }
                };

                VisualElement keyEntryContainer = new() { style = { marginBottom = 2 } };
                DrawUIForType(keyType, keyProp, keyEntryContainer);
                keyEntryContainer.SetEnabled(false);
                VisualElement valueEntryContainer = new() { style = { marginBottom = 2 } };
                DrawUIForType(valueType, valueProp, valueEntryContainer);
                valueEntryContainer.SetEnabled(false);

                Button removeButton = new(() =>
                {
                    if (index >= listProperty.arraySize) return;
                    listProperty.DeleteArrayElementAtIndex(index);
                    property.serializedObject.ApplyModifiedProperties();
                })
                {
                    text = "Remove",
                    style = { alignSelf = Align.FlexEnd }
                };

                entryRow.Add(keyEntryContainer);
                entryRow.Add(valueEntryContainer);
                entryRow.Add(removeButton);
                listContainer.Add(entryRow);
            }
        }
    }
    
    static void DrawUIForType(Type typeToDrawUIFor, SerializedProperty property, VisualElement container)
    {
        BuildUIForType builderDelegate = GetOrCacheUIBuilder(typeToDrawUIFor);
        builderDelegate?.Invoke(property, container);
    }

    static BuildUIForType GetOrCacheUIBuilder(Type typeToDrawUIFor)
    {
        if (typeToDrawUIFor == null)
            return null;
    
        if (s_UIBuilderCache.TryGetValue(typeToDrawUIFor, out BuildUIForType cached))
            return cached;
        
        BuildUIForType builder = CreateBuilderForType();
        s_UIBuilderCache[typeToDrawUIFor] = builder;
        return builder;
    }

    static BuildUIForType CreateBuilderForType()
    {
        return (prop, typeContainer) =>
        {
            PropertyDrawer drawer = CustomNamespace.Editor.PropertyDrawerCache.CreateDrawerForProperty(prop, typeof(SerializableDictionaryDrawer));
            VisualElement customUI = drawer?.CreatePropertyGUI(prop);
            if (customUI != null)
            {
                typeContainer.Add(customUI);
                return;
            }
            PropertyField field = new(prop, "");
            field.BindProperty(prop);
            typeContainer.Add(field);
        };
    }
    
    static void CopyPropertyValue(SerializedProperty source, SerializedProperty dest)
    {
        if (source == null || dest == null) return;
        dest.boxedValue = source.boxedValue;
    }
    
    static Type GetFieldType(SerializedProperty prop)
    {
        string path = prop.propertyPath.Replace(".Array.data[", "[");
        string[] elements = path.Split('.');

        Type currentType = prop.serializedObject.targetObject.GetType();

        foreach (string t in elements)
        {
            string element = t;
            if (element.EndsWith("]"))
            {
                currentType = currentType is { IsArray: true } ? currentType.GetElementType() : currentType?.GetGenericArguments()[0];
                element = element[..element.IndexOf('[')];
            }

            if (currentType == null) continue;
            FieldInfo field = currentType.GetField(element, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            
            if (field == null)
            {
                return currentType;
            }

            currentType = field.FieldType;
        }

        return currentType;
    }

    static void ClearPropertyValue(SerializedProperty prop, Type targetType)
    {
        if (prop == null || targetType == null) return;
        if (!targetType.IsValueType || prop.propertyType == SerializedPropertyType.ManagedReference)
        {
            try
            {
                prop.boxedValue = null;
            }
            catch
            {
                ClearStructChildren(prop);
            }
        }
        else 
        {
            ClearStructChildren(prop);
        }
    }
    
    static void ClearStructChildren(SerializedProperty prop)
    {
        SerializedProperty iterator = prop.Copy();
        SerializedProperty end = prop.GetEndProperty();

        if (!iterator.Next(true)) return; 
        do
        {
            if (SerializedProperty.EqualContents(iterator, end)) break;
            Type fieldType = GetFieldType(iterator);
            if (iterator.propertyType == SerializedPropertyType.Generic)
            {
                ClearStructChildren(iterator); 
            }
            else
            {
                try
                {
                    object defaultValue = fieldType.IsValueType && fieldType != typeof(void) ? Activator.CreateInstance(fieldType) : null;
                    iterator.boxedValue = defaultValue;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to reset field {iterator.name} of type {fieldType.Name}. Error: {e.Message}");
                }
            }
        }
        while (iterator.Next(false));
    }
}
#endif