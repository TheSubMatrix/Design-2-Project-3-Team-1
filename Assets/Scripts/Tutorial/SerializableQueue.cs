using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

[System.Serializable]
public class SerializableQueue<T> : ISerializationCallbackReceiver, IEnumerable<T>
{
    [FormerlySerializedAs("serializedData")] [SerializeField]
    List<T> m_serializedData = new();
    [System.NonSerialized]
    Queue<T> m_value = new();
    public void OnBeforeSerialize()
    {
        if (m_value == null)
        {
            return;
        }

        m_serializedData.Clear();
        foreach (T item in m_value)
        {
            m_serializedData.Add(item);
        }
    }

    public void OnAfterDeserialize()
    {
        m_value = new Queue<T>();

        if (m_serializedData == null) return;
        foreach (T item in m_serializedData)
        {
            m_value.Enqueue(item);
        }
    }
    public void Enqueue(T item)
    {
        m_value.Enqueue(item);
    }
    public T Dequeue()
    {
        return m_value.Dequeue();
    }

    public T Peek()
    {
        return m_value.Peek();
    }
    public void Clear()
    {
        m_value.Clear();
    }
    public int Count => m_value.Count;
    public bool Contains(T item)
    {
        return m_value.Contains(item);
    }
    public IEnumerator<T> GetEnumerator()
    {
        return m_value.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}