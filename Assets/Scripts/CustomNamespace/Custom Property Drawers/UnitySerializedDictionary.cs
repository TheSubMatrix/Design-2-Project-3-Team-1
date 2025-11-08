using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [FormerlySerializedAs("list")] 
    [SerializeField]
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

    public void Rebuild()
    {
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
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        Type[] genericArgs = fieldInfo.FieldType.GetGenericArguments();
        if (genericArgs.Length != 2)
            return new Label("Error: Invalid SerializableDictionary type");
        Type valueType = genericArgs[1];

        VisualElement container = new();
        SerializedProperty listProperty = property.FindPropertyRelative("m_list");
        SerializedProperty stagingProperty = property.FindPropertyRelative("m_stagingEntry");
        SerializedProperty stagingKey = stagingProperty.FindPropertyRelative("Key");

        Foldout foldout = new()
        {
            text = $"{property.displayName} ({listProperty.arraySize} entries)",
            value = property.isExpanded
        };
        foldout.RegisterValueChangedCallback(evt => property.isExpanded = evt.newValue);

        // --- Staging area ---
        VisualElement newEntrySection = new();

        DrawUIWithLabel(stagingKey, newEntrySection, "Key", true);

        Button addButton = new(() =>
        {
            int existingIndex = -1;
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                if (!SerializedProperty.DataEquals(element.FindPropertyRelative("Key"), stagingKey)) continue;
                existingIndex = i;
                break;
            }

            if (existingIndex >= 0)
            {
                property.serializedObject.ApplyModifiedProperties();
                return;
            }

            int index = listProperty.arraySize;
            listProperty.arraySize++;
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();

            SerializedProperty elementNew = listProperty.GetArrayElementAtIndex(index);
            SerializedProperty keyProp = elementNew.FindPropertyRelative("Key");
            SerializedProperty valueProp = elementNew.FindPropertyRelative("Value");

            keyProp.boxedValue = stagingKey.boxedValue;
            if (typeof(UnityEngine.Object).IsAssignableFrom(valueType))
                valueProp.objectReferenceValue = null;
            else
                valueProp.boxedValue = Activator.CreateInstance(valueType);

            property.serializedObject.ApplyModifiedProperties();
        })
        {
            text = "Add / Select",
            style = { alignSelf = Align.FlexEnd, width = 100 }
        };
        newEntrySection.Add(addButton);

        // Divider
        newEntrySection.Add(new VisualElement
        {
            style = {
                height = 1,
                marginTop = 6,
                marginBottom = 6,
                backgroundColor = new StyleColor(EditorGUIUtility.isProSkin 
                    ? new Color(0.4f,0.4f,0.4f,0.35f) 
                    : new Color(0.25f,0.25f,0.25f,0.35f))
            }
        });

        foldout.Add(newEntrySection);

        // --- Existing entries list ---
        VisualElement listContainer = new();
        foldout.Add(listContainer);
        container.Add(foldout);

        RebuildList();
        
        int lastArraySize = listProperty.arraySize;
        container.TrackPropertyValue(listProperty, p =>
        {
            if (p.arraySize == lastArraySize) return;
            lastArraySize = p.arraySize;
            foldout.text = $"{property.displayName} ({p.arraySize} entries)";
            RebuildList();
        });

        return container;

        void RebuildList()
        {
            listContainer.Clear();

            if (listProperty.arraySize == 0)
            {
                listContainer.Add(new Label("No entries") { style = { unityFontStyleAndWeight = FontStyle.Italic } });
                return;
            }

            for (int i = 0; i < listProperty.arraySize; i++)
            {
                int idx = i;
                SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                SerializedProperty keyProp = element.FindPropertyRelative("Key");
                SerializedProperty valProp = element.FindPropertyRelative("Value");

                VisualElement row = new()
                {
                    style = {
                        flexDirection = FlexDirection.Column,
                        paddingTop = 2,
                        paddingBottom = 2,
                        paddingLeft = 2,
                        paddingRight = 2,
                        marginBottom = 3,
                        borderBottomWidth = 1,
                        borderBottomColor = new StyleColor(new Color(0.1f,0.1f,0.1f,0.3f))
                    }
                };

                DrawUIWithLabel(keyProp, row, "Key", false);
                DrawUIWithLabel(valProp, row, "Value", true);

                Button remove = new(() =>
                {
                    listProperty.DeleteArrayElementAtIndex(idx);
                    property.serializedObject.ApplyModifiedProperties();
                })
                { text = "Remove", style = { alignSelf = Align.FlexEnd } };
                row.Add(remove);

                listContainer.Add(row);
            }
        }
    }

    static void DrawUIWithLabel(SerializedProperty prop, VisualElement container, string label, bool enabled)
    {
        PropertyField field = new(prop, label);
        field.BindProperty(prop);
        field.SetEnabled(enabled);
        container.Add(field);
    }

}
#endif