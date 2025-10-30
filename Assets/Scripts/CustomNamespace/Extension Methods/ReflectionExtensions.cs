using System;
using System.Reflection;
using UnityEngine;

namespace CustomNamespace.Extensions
{
    /// <summary>
    /// Provides extension methods that use reflection.
    /// </summary>
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Gets a private value from an object using reflection
        /// This is usually bad, but unity is forcing my hand
        /// </summary>
        /// <remarks>
        /// Using reflection to access private members can have performance implications and may break if the internal structure of the target object changes.
        /// Consider alternative approaches if possible.
        /// </remarks>
        /// <typeparam name="T">The type of variable that you are retrieving</typeparam>
        /// <param name="obj">object to get values from</param>
        /// <param name="name">The name of the variable you wish to retrieve</param>
        /// <returns>the value of the variable by the name passed into the method</returns>
        public static T GetFieldValue<T>(this object obj, string name)
        {
            // Set the flags so that private and public fields from instances will be found
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo field = obj.GetType().GetField(name, bindingFlags);
            return (T)field?.GetValue(obj);
        }

        /// <summary>
        /// Sets the value of a private or public field on an object using reflection.
        /// This is usually bad practice as it breaks encapsulation, but may be necessary for
        /// interacting with internal Unity or third-party APIs.
        /// </summary>
        /// <remarks>
        /// Using reflection to modify private members can have performance implications and may break if the internal structure of the target object changes.
        /// Use with caution and consider alternative approaches if possible.
        /// </remarks>
        /// <typeparam name="T">The type of the value being set.</typeparam>
        /// <param name="obj">The object whose field value will be modified.</param>
        /// <param name="name">The name of the field to set.</param>
        /// <param name="value">The new value to assign to the field.</param>
        public static void SetFieldValue<T>(this object obj, string name, T value)
        {
            // Set the flags so that private and public fields from instances will be found
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo field = obj.GetType().GetField(name, bindingFlags);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogError($"Field '{name}' not found on object of type '{obj.GetType().Name}'. Cannot set value using reflection.");
            }
        }
        
        /// <summary>
        /// Gets the value of a field using reflection, supporting nested paths and array/list indexing.
        /// </summary>
        /// <param name="obj">The object to get the field value from</param>
        /// <param name="fieldPath">The field path (e.g., "parent.child.field" or "items[0].name")</param>
        /// <returns>The field value, or null if not found</returns>
        public static object GetFieldValue(this object obj, string fieldPath)
        {
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        
            // Handle nested paths (e.g., "parent.child.field")
            string path = fieldPath.Replace(".Array.data[", "[");
            string[] elements = path.Split('.');
        
            object currentObj = obj;
            foreach (string element in elements)
            {
                if (currentObj == null)
                    return null;

                if (element.Contains("["))
                {
                    // Handle array/list indexing
                    string elementName = element[..element.IndexOf("[", StringComparison.Ordinal)];
                    int index = int.Parse(element.Substring(
                        element.IndexOf("[", StringComparison.Ordinal) + 1, 
                        element.IndexOf("]", StringComparison.Ordinal) - element.IndexOf("[", StringComparison.Ordinal) - 1));
                
                    FieldInfo field = currentObj.GetType().GetField(elementName, bindingFlags);
                    if (field == null)
                        return null;

                    object array = field.GetValue(currentObj);
                    if (array is System.Collections.IList list)
                    {
                        currentObj = list[index];
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    FieldInfo field = currentObj.GetType().GetField(element, bindingFlags);
                    if (field == null)
                        return null;

                    currentObj = field.GetValue(currentObj);
                }
            }
        
            return currentObj;
        }
        
        /// <summary>
        /// Unwraps array and List types to get the element type.
        /// Returns the original type if it's not a collection.
        /// </summary>
        /// <param name="type">The type to unwrap</param>
        /// <returns>The element type if it's an array or List, otherwise the original type</returns>
        public static Type UnwrapCollectionType(this Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }
            
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
            {
                return type.GetGenericArguments()[0];
            }
            
            return type;
        }
        
        /// <summary>
        /// This will create an identical copy of a class including its private variables. It will also create subclasses of the generated class if they are not a <see cref="UnityEngine.Object"/>. If the class is a <see cref="UnityEngine.Object"/>, a reference will instead be passed.
        /// </summary>
        /// <remarks>Slow and not to be used during runtime due to reflection terribleness</remarks>
        /// <typeparam name="T">The type to return the object as</typeparam>
        /// <param name="input">The object to copy</param>
        /// <returns>A new copy of the object with identical values to the object passed in</returns>
        public static T CopyWithAllValues<T>(this T input)
        {
            if (input is null)
                return default;
            Type type = input.GetType();
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo[] properties = type.GetFields(bindingFlags);
            //Create an object instance
            T clonedObj = (T)Activator.CreateInstance(type);
            //Loop through the stupid properties to copy them
            foreach (FieldInfo property in properties)
            {
                object value = property.GetValue(input);
                //I really hate this I can't even cache the type without a null reference here
                //Create child class instance if needed
                //Let it be known that this is currently the single worst if statement I've ever written
                if
                (
                    value is not null
                    && value.GetType().IsClass
                    && !value.GetType().FullName!.StartsWith("System.")
                    && (!value.GetType().IsSubclassOf(typeof(UnityEngine.Object)))
                    && (value.GetType().Namespace is not "AK")
                    && value.GetType().IsByRef
                )
                {
                    property.SetValue(clonedObj, value.CopyWithAllValues());
                }
                property.SetValue(clonedObj, value);
            }
            return clonedObj;
        }
    }
}