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
public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
{
    [SerializeReference, HideInInspector] SerializableType m_keyType= typeof(TKey);
    [SerializeReference, HideInInspector] SerializableType m_valueType = typeof(TValue);
    
    [SerializeField]
    List<SerializableKeyValuePair<TKey, TValue>> m_list = new();
    public Dictionary<TKey, TValue> Dictionary = new();
    public static implicit operator Dictionary<TKey, TValue>(SerializableDictionary<TKey, TValue> dictionary)
    {
        return dictionary.Dictionary;
    }
    public static implicit operator SerializableDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
    {
        return new SerializableDictionary<TKey, TValue>(dictionary); 
    }
    public SerializableDictionary() { }
    public SerializableDictionary(Dictionary<TKey, TValue> dictionary)
    {
        Dictionary = new Dictionary<TKey, TValue>(dictionary);
    }
    public void OnBeforeSerialize()
    {
        m_list.Clear();
        foreach (KeyValuePair<TKey, TValue> kvp in Dictionary)
        {
            m_list.Add(new SerializableKeyValuePair<TKey, TValue>(kvp.Key, kvp.Value));
        }
    }

    public void OnAfterDeserialize()
    {
        Dictionary.Clear();
        foreach (SerializableKeyValuePair<TKey, TValue> kvp in m_list.Where(kvp => !Dictionary.ContainsKey(kvp.Key)))
        {
            Dictionary.Add(kvp.Key, kvp.Value);
        }
    }
    
    internal void UpdateList(List<SerializableKeyValuePair<TKey, TValue>> list)
    {
        m_list = list;
    }
}


#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
public class SerializableDictionaryDrawer : PropertyDrawer
{
    object m_keyToAdd;
    object m_valueToAdd;
    List<object> m_keyValuePairsInDictionary;
    Type m_genericKeyValuePairType;
    Type m_genericListKeyValuePairType;
    
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        if (m_keyValuePairsInDictionary is null)
        {
            m_genericKeyValuePairType = typeof(SerializableKeyValuePair<,>).MakeGenericType((property.FindPropertyRelative("m_keyType").managedReferenceValue as SerializableType)?.Type, (property.FindPropertyRelative("m_valueType").managedReferenceValue as SerializableType)?.Type);
            m_genericListKeyValuePairType = typeof(List<>).MakeGenericType(m_genericKeyValuePairType);
            m_keyValuePairsInDictionary = Activator.CreateInstance(m_genericListKeyValuePairType, ((Convert.ChangeType(property.FindPropertyRelative("m_list"), m_genericKeyValuePairType))as List<object>)?.ToArray()) as List<object>;
        }
        
        VisualElement container = new();
        return container;
    }

    void AddToDictionary()
    {
        
    }
}
#endif
