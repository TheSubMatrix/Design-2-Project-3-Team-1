using System;
using UnityEngine;
using UnityEngine.Events;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor.Events;
#endif


namespace CustomNamespace.GenericDatatypes
{
    [Serializable]
    public class Observer<T>
    {
        [SerializeField] T m_value;
        [SerializeField] UnityEvent<T> m_onValueChanged;

        public T Value
        {
            get => m_value;
            set => Set(value);
        }

        public static implicit operator T(Observer<T> observer) => observer.Value;

        public Observer(T value, UnityAction<T> callback = null)
        {
            Value = value;
            m_onValueChanged = new UnityEvent<T>();
            if (callback is not null) m_onValueChanged.AddListener(callback);
        }

        void Set(T value)
        {
            if (Equals(m_value, value)) return;
            m_value = value;
            Invoke();
        }

        void Invoke()
        {
            m_onValueChanged.Invoke(m_value);
        }

        public void AddListener(UnityAction<T> callback)
        {
            if (callback is null) return;
            m_onValueChanged ??= new UnityEvent<T>();
            #if UNITY_EDITOR
                UnityEventTools.AddPersistentListener(m_onValueChanged, callback);
            #else
                m_onValueChanged.AddListener(callback);
            #endif
        }

        public void RemoveListener(UnityAction<T> callback)
        {
            if (callback is null) return;
            m_onValueChanged ??= new UnityEvent<T>();
            #if UNITY_EDITOR
                UnityEventTools.RemovePersistentListener(m_onValueChanged, callback);
            #else
                m_onValueChanged.RemoveListener(callback);
            #endif
        }

        public void RemoveAllListeners()
        {
            #if UNITY_EDITOR
                FieldInfo fieldInfo =
                typeof(UnityEventBase).GetField("m_PersistentCalls", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo == null) return;
                object value = fieldInfo.GetValue(m_onValueChanged);
                value.GetType().GetMethod("Clear")?.Invoke(value, null);
            #else
                m_onValueChanged.RemoveAllListeners();
            #endif
        }

        public void Dispose()
        {
            RemoveAllListeners();
            m_onValueChanged = null;
            m_value = default;
        }

    }
}