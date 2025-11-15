#if UNITY_EDITOR
// Attribute to filter types shown in the inspector for VFX properties
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.VFX;

namespace VFXSystem
{
    public class VFXPropertyTypeFilterAttribute : PropertyAttribute
    {
        public static bool IsValidVFXPropertyType(Type type)
        {
            if (type == null || type.IsAbstract || type.IsInterface) return false;
            
            // Check if VisualEffect has a Set method for this type
            MethodInfo[] methods = typeof(VisualEffect).GetMethods(BindingFlags.Public | BindingFlags.Instance);

            return (from method in methods 
                where method.Name.StartsWith("Set") 
                select method.GetParameters() into parameters 
                where parameters.Length == 2 where parameters[0].ParameterType == typeof(string) 
                select parameters[1].ParameterType).Any(paramType => paramType == type || paramType.IsAssignableFrom(type));
        }
    }
}
#endif