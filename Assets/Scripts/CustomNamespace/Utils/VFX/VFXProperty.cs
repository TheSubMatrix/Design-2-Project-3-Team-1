using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.VFX;

namespace VFXSystem
{
    [Serializable]
    public class VFXProperty
    {
        public string Name;
        public SerializableType ValueType;
        
        [SerializeReference]
        public object Value;

        public VFXProperty() { }

        public VFXProperty(string name, object value)
        {
            Name = name;
            Value = value;
            ValueType = value?.GetType();
        }

        public void ApplyToVisualEffect(VisualEffect vfx)
        {
            if (string.IsNullOrEmpty(Name) || Value == null) return;

            Type valueType = ValueType?.Type ?? Value.GetType();
            
            // Get all Set methods from VisualEffect
            MethodInfo[] methods = typeof(VisualEffect).GetMethods(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (MethodInfo method in methods)
            {
                // Look for Set methods with 2 parameters (string name, T value)
                if (!method.Name.StartsWith("Set")) continue;
                
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 2) continue;
                if (parameters[0].ParameterType != typeof(string)) continue;
                
                Type paramType = parameters[1].ParameterType;
                
                // Check if the value type matches or is assignable to the parameter type
                if (paramType != valueType && !paramType.IsAssignableFrom(valueType)) continue;
                // Check if the property exists using the corresponding Has method
                string hasMethodName = method.Name.Replace("Set", "Has");
                MethodInfo hasMethod = typeof(VisualEffect).GetMethod(hasMethodName, new[] { typeof(string) });

                if (hasMethod == null) continue;
                bool hasProperty = (bool)hasMethod.Invoke(vfx, new object[] { Name });
                if (!hasProperty) continue;
                try
                {
                    method.Invoke(vfx, new object[] { Name, Value });
                    return; // Successfully applied
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to set VFX property '{Name}': {e.Message}");
                }
            }
            
            Debug.LogWarning($"Could not find appropriate setter for VFX property '{Name}' of type {valueType?.Name}");
        }

        public T GetValue<T>()
        {
            if (Value is T typedValue)
                return typedValue;
            
            return default;
        }
        
        public void SetValue(object value)
        {
            Value = value;
            ValueType = value?.GetType();
        }
    }
}