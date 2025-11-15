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
        Observer() { }
        Observer(T value) { m_value = value;}
        
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
            m_onValueChanged = new UnityEvent<T>();
            if (callback is not null) m_onValueChanged.AddListener(callback);
            Value = value;
        }
        public void SetValueWithoutNotify(T value)
        {
            m_value = value;
        }
        
        void Set(T value)
        {
            if (Equals(m_value, value)) return;
            m_value = value;
            Notify();
        }

        public void Notify()
        {
            m_onValueChanged?.Invoke(m_value);
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
    public static class ObserverExtensions
    {
        /// <summary>
        /// Updates the observer's value using a builder pattern function and forces notification
        /// </summary>
        /// <typeparam name="T">The type of the observed value</typeparam>
        /// <param name="observer">The observer to update</param>
        /// <param name="builder">A function that takes the current value and returns the modified value</param>
        /// <returns>The observer instance for further chaining</returns>
        public static Observer<T> Update<T>(this Observer<T> observer, Func<T, T> builder)
        {
            T updatedValue = builder(observer.Value);
            observer.SetValueWithoutNotify(updatedValue);
            observer.Notify();
            return observer;
        }
    }
}
