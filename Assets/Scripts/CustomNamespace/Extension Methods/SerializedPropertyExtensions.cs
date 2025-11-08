using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CustomNamespace.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="SerializedProperty"/> class in the Unity Editor.
    /// </summary>
    public static class SerializedPropertyExtensions
    {
        #if UNITY_EDITOR
        // Delegate for the private static method 'GetFieldInfoAndStaticTypeFromProperty' in 'ScriptAttributeUtility'.
        private delegate FieldInfo GetFieldInfoAndStaticTypeFromProperty(SerializedProperty aProperty, out Type aType);
        private static GetFieldInfoAndStaticTypeFromProperty getFieldInfoAndStaticTypeFromProperty;

        /// <summary>
        /// Uses reflection to get the <see cref="FieldInfo"/> and static <see cref="Type"/> of the field that the <see cref="SerializedProperty"/> represents.
        /// This relies on accessing a private internal method of Unity's Editor code.
        /// </summary>
        /// <param name="prop">The <see cref="SerializedProperty"/> to get the field information from.</param>
        /// <param name="type">When this method returns, contains the static <see cref="Type"/> of the field.</param>
        /// <returns>The <see cref="FieldInfo"/> of the field represented by the <see cref="SerializedProperty"/>, or null if reflection fails.</returns>
        public static FieldInfo GetFieldInfoAndStaticType(this SerializedProperty prop, out Type type)
        {
            // Lazy initialization of the delegate to the internal Unity method.
            if (getFieldInfoAndStaticTypeFromProperty != null)
                return getFieldInfoAndStaticTypeFromProperty(prop, out type);
            // Iterate through all loaded assemblies.
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Iterate through all types in the current assembly.
                foreach (Type t in assembly.GetTypes())
                {
                    // Look for the internal Unity class 'ScriptAttributeUtility'.
                    if (t.Name != "ScriptAttributeUtility") continue;
                    // Get the non-public static method 'GetFieldInfoAndStaticTypeFromProperty'.
                    MethodInfo mi = t.GetMethod("GetFieldInfoAndStaticTypeFromProperty", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    // Create a delegate to this method.
                    getFieldInfoAndStaticTypeFromProperty = (GetFieldInfoAndStaticTypeFromProperty)Delegate.CreateDelegate(typeof(GetFieldInfoAndStaticTypeFromProperty), mi);
                    break; // Found the method, exit the inner loop.
                }
                if (getFieldInfoAndStaticTypeFromProperty != null) break; // Found the method, exit the outer loop.
            }

            // If the reflection failed to find the method.
            if (getFieldInfoAndStaticTypeFromProperty != null)
                return getFieldInfoAndStaticTypeFromProperty(prop, out type);
            UnityEngine.Debug.LogError("GetFieldInfoAndStaticType::Reflection failed!");
            type = null;
            return null;
            // Invoke the delegate to get the FieldInfo and Type.
        }

        /// <summary>
        /// Gets a custom attribute of type <typeparamref name="T"/> applied to the field that the <see cref="SerializedProperty"/> represents.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="Attribute"/> to retrieve.</typeparam>
        /// <param name="prop">The <see cref="SerializedProperty"/> to get the attribute from.</param>
        /// <returns>The custom attribute of type <typeparamref name="T"/> if found on the field, otherwise null.</returns>
        public static T GetCustomAttributeFromProperty<T>(this SerializedProperty prop) where T : Attribute
        {
            // Get the FieldInfo of the property.
            FieldInfo info = prop.GetFieldInfoAndStaticType(out _);
            // If FieldInfo is found, get the custom attribute.
            return info?.GetCustomAttribute<T>();
        }

        /// <summary>
        /// Gets an enumerable collection of the child properties of a given <see cref="SerializedProperty"/>.
        /// This method iterates through the direct children of the property in the Inspector.
        /// </summary>
        /// <param name="serializedProperty">The parent <see cref="SerializedProperty"/> whose children to retrieve.</param>
        /// <returns>An <see cref="IEnumerable{SerializedProperty}"/> that yields the child properties.</returns>
        public static IEnumerable<SerializedProperty> GetChildren(this SerializedProperty serializedProperty)
        {
            // Create copies of the SerializedProperty for iteration.
            SerializedProperty currentProperty = serializedProperty.Copy();
            SerializedProperty nextSiblingProperty = serializedProperty.Copy();
            {
                // Move the 'nextSiblingProperty' to the next property at the same level.
                nextSiblingProperty.Next(false);
            }

            // Move 'currentProperty' to its first child.
            if (!currentProperty.Next(true)) yield break;
            // Iterate through the children until the 'currentProperty' reaches the 'nextSiblingProperty'
            // (meaning we've iterated through all direct children).
            do
            {
                // If the current property is the same as the next sibling, we've reached the end of the children.
                if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
                    break;

                // Yield the current child property.
                yield return currentProperty;
            }
            // Move to the next sibling of the current child.
            while (currentProperty.Next(false));
        }
        
        /// <summary>
        /// Gets the FieldInfo for a SerializedProperty, handling nested properties and collections.
        /// This is a simpler alternative to GetFieldInfoAndStaticType when you don't need the static type.
        /// </summary>
        /// <param name="property">The SerializedProperty to get FieldInfo for</param>
        /// <returns>FieldInfo for the property, or null if not found</returns>
        public static FieldInfo GetFieldInfo(this SerializedProperty property)
        {
            UnityEngine.Object targetObject = property.serializedObject.targetObject;
            Type targetType = targetObject.GetType();
            
            string path = property.propertyPath.Replace(".Array.data[", "[");
            string[] elements = path.Split('.');
            
            FieldInfo fieldInfo = null;
            foreach (string element in elements)
            {
                if (targetType == null)
                    continue;
                
                if (element.Contains("["))
                {
                    string elementName = element[..element.IndexOf("[", StringComparison.Ordinal)];
                    fieldInfo = targetType.GetField(elementName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    
                    if (fieldInfo != null)
                    {
                        targetType = fieldInfo.FieldType.UnwrapCollectionType();
                    }
                }
                else
                {
                    fieldInfo = targetType.GetField(element,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    
                    if (fieldInfo != null)
                    {
                        targetType = fieldInfo.FieldType;
                    }
                }
            }
            
            return fieldInfo;
        }

        /// <summary>
        /// Delegate for the internal static method.
        /// Uses public types for arguments to avoid needing GetInternalPtr.
        /// </summary>
        private delegate bool InternalCopySerializedPropertyDelegate(SerializedProperty dest, SerializedProperty src);
        private static InternalCopySerializedPropertyDelegate internalCopySerializedProperty;

        /// <summary>
        /// Reliably copies the serialized data from one property to another, including object references, 
        /// by accessing Unity's internal serialization method via reflection.
        /// </summary>
        /// <param name="destProperty">The destination property (the one to be changed).</param>
        /// <param name="srcProperty">The source property (the one whose value will be copied).</param>
        /// <returns>True if the copy was successful, false otherwise.</returns>
        public static bool CopySerializedPropertyDataFrom(this SerializedProperty destProperty, SerializedProperty srcProperty)
        {
            // --- Lazy Initialization of the Copy Delegate ---
            if (internalCopySerializedProperty != null)
                return internalCopySerializedProperty != null &&
                       internalCopySerializedProperty(destProperty, srcProperty);
            Type serializedPropertyType = typeof(SerializedProperty);
                
            // Find the internal static method named 'InternalCopySerializedProperty'.
            // It must take two SerializedProperty arguments.
            MethodInfo mi = serializedPropertyType.GetMethod(
                "InternalCopySerializedProperty", 
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, 
                null, 
                new Type[] { serializedPropertyType, serializedPropertyType }, // Signature check
                null
            );
                
            if (mi != null)
            {
                try
                {
                    // Create the delegate using the method info.
                    internalCopySerializedProperty = (InternalCopySerializedPropertyDelegate)Delegate.CreateDelegate(
                        typeof(InternalCopySerializedPropertyDelegate), 
                        mi
                    );
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create delegate for InternalCopySerializedProperty: {ex.Message}");
                    return false;
                }
            }
            else
            {
                Debug.LogError("Reflection failed: Could not find internal method 'InternalCopySerializedProperty' with the expected signature.");
                return false;
            }
            return internalCopySerializedProperty != null && internalCopySerializedProperty(destProperty, srcProperty);
        }
        #endif
    }
}