using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

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