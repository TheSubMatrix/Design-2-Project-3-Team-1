#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomNamespace.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace VFXSystem
{
    [CustomPropertyDrawer(typeof(VFXProperty))]
    public class VFXPropertyDrawer : PropertyDrawer
    {
        static Type[] s_validVFXTypes;
        static string[] s_validVFXTypeNames;
        
        static void InitializeValidTypes()
        {
            if (s_validVFXTypes != null) return;
            
            // Get all Set methods from VisualEffect and extract parameter types
            MethodInfo[] methods = typeof(VisualEffect).GetMethods(BindingFlags.Public | BindingFlags.Instance);
            HashSet<Type> typeSet = new();
            
            foreach (MethodInfo method in methods)
            {
                if (!method.Name.StartsWith("Set")) continue;
                
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 2) continue;
                if (parameters[0].ParameterType != typeof(string)) continue;
                
                Type paramType = parameters[1].ParameterType;
                if (!paramType.IsAbstract && !paramType.IsInterface)
                {
                    typeSet.Add(paramType);
                }
            }
            
            s_validVFXTypes = typeSet.OrderBy(t => t.Name).ToArray();
            s_validVFXTypeNames = s_validVFXTypes.Select(t => t.Name).ToArray();
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            InitializeValidTypes();
            
            VisualElement container = new()
            {
                style =
                {
                    marginBottom = 4,
                    paddingTop = 2,
                    paddingBottom = 2,
                    paddingLeft = 2,
                    paddingRight = 2,
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(0.1f, 0.1f, 0.1f, 0.3f)
                }
            };

            SerializedProperty nameProp = property.FindPropertyRelative("Name");
            SerializedProperty valueTypeProp = property.FindPropertyRelative("ValueType");
            SerializedProperty valueProp = property.FindPropertyRelative("Value");
            SerializedProperty assemblyQualifiedNameProp = valueTypeProp.FindPropertyRelative("m_assemblyQualifiedName");
            
            // Property name field
            TextField nameField = new("Property Name")
            {
                value = nameProp.stringValue
            };
            nameField.RegisterValueChangedCallback(evt =>
            {
                nameProp.stringValue = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
            });
            container.Add(nameField);
            
            // Type selector
            int currentTypeIndex = GetTypeIndex(assemblyQualifiedNameProp.stringValue);
            PopupField<string> typePopup = new(
                "Value Type",
                s_validVFXTypeNames.ToList(),
                currentTypeIndex >= 0 ? currentTypeIndex : 0
            );
            
            typePopup.RegisterValueChangedCallback(evt =>
            {
                int selectedIndex = Array.IndexOf(s_validVFXTypeNames, evt.newValue);
                if (selectedIndex < 0 || selectedIndex >= s_validVFXTypes.Length) return;
                
                Type newType = s_validVFXTypes[selectedIndex];
                assemblyQualifiedNameProp.stringValue = newType.AssemblyQualifiedName;
                
                // Reset value to default for a new type
                try
                {
                    valueProp.managedReferenceValue = CreateDefaultValue(newType);
                }
                catch
                {
                    valueProp.managedReferenceValue = null;
                }
                
                property.serializedObject.ApplyModifiedProperties();
                
                // Rebuild the value container with the appropriate drawer
                VisualElement valueContainer = container.Q<VisualElement>("valueContainer");
                if (valueContainer == null) return;
                valueContainer.Clear();
                BuildValueField(valueContainer, valueProp, newType);
            });
            container.Add(typePopup);
            
            // Value field container
            VisualElement valueContainer = new() { name = "valueContainer" };
            Type currentType = GetTypeFromAssemblyQualifiedName(assemblyQualifiedNameProp.stringValue);
            if (currentType != null)
            {
                BuildValueField(valueContainer, valueProp, currentType);
            }
            container.Add(valueContainer);
            
            return container;
        }

        static void BuildValueField(VisualElement container, SerializedProperty valueProp, Type valueType)
        {
            if (valueType == null) return;
            
            // Label for the value field
            Label valueLabel = new("Value")
            {
                style = { 
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = 4,
                    marginBottom = 2
                }
            };
            container.Add(valueLabel);
            
            // Ensure we have a value instance
            if (valueProp.managedReferenceValue == null)
            {
                valueProp.managedReferenceValue = CreateDefaultValue(valueType);
                valueProp.serializedObject.ApplyModifiedProperties();
            }
            
            // Check if there's a custom drawer for this type
            Type drawerType = PropertyDrawerCache.GetDrawerType(valueType);
            
            if (drawerType != null)
            {
                // Use a PropertyField which will use the custom drawer
                PropertyField propertyField = new(valueProp, string.Empty);
                propertyField.BindProperty(valueProp);
                container.Add(propertyField);
            }
            else
            {
                // No custom drawer - use PropertyDrawerCache to create fields for all children
                VisualElement fieldsContainer = new();
                PropertyDrawerCache.CreateDefaultPropertyFields(valueProp, fieldsContainer);
                container.Add(fieldsContainer);
            }
        }

        static int GetTypeIndex(string assemblyQualifiedName)
        {
            if (string.IsNullOrEmpty(assemblyQualifiedName)) return 0;
            
            Type type = Type.GetType(assemblyQualifiedName);
            return type == null ? 0 : Array.IndexOf(s_validVFXTypes, type);
        }

        static Type GetTypeFromAssemblyQualifiedName(string assemblyQualifiedName)
        {
            return string.IsNullOrEmpty(assemblyQualifiedName) ? null : Type.GetType(assemblyQualifiedName);
        }

        static object CreateDefaultValue(Type type)
        {
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return null;
            
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            
            if (type == typeof(string))
                return string.Empty;
            
            if (type == typeof(AnimationCurve))
                return new AnimationCurve();
            
            if (type == typeof(Gradient))
                return new Gradient();
            
            // Try to create an instance with the default constructor
            try
            {
                return Activator.CreateInstance(type);
            }
            catch
            {
                return null;
            }
        }
    }
}
#endif