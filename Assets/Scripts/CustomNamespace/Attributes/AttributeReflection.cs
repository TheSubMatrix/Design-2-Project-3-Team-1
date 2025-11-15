using System.Reflection;
using UnityEngine;

public static class AttributeReflection
{
    /// <summary>
    /// Checks if a field has a specific PropertyAttribute applied to it,
    /// and optionally returns the attribute instance.
    /// </summary>
    /// <typeparam name="TAttribute">The type of the PropertyAttribute to check for.</typeparam>
    /// <param name="targetObject">The object instance containing the field.</param>
    /// <param name="fieldName">The name of the field to check.</param>
    /// <param name="attributeInstance">Output parameter: The found attribute instance, or null if not found.</param>
    /// <returns>True if the attribute is found, false otherwise.</returns>
    public static bool TryGetFieldAttribute<TAttribute>(object targetObject, string fieldName, out TAttribute attributeInstance) where TAttribute : PropertyAttribute
        // Constrain to PropertyAttribute or derived
    {
        attributeInstance = null;
        if (targetObject == null)
        {
            return false;
        }

        // Get the FieldInfo using reflection
        FieldInfo fieldInfo = targetObject.GetType().GetField( fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );

        if (fieldInfo != null)
        {
            // Get the attribute instance (searching up inheritance hierarchy)
            attributeInstance = fieldInfo.GetCustomAttribute<TAttribute>(true);
            return attributeInstance != null;
        }
        return false;
    }
}

