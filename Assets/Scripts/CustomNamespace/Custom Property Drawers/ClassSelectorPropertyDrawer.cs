#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomNamespace.Editor;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using CustomNamespace.Extensions;
using UnityEngine;

[CustomPropertyDrawer(typeof(ClassSelectorAttribute))]
public class ClassSelectorPropertyDrawer : PropertyDrawer
{
    ClassSelectorAttribute m_attributeData;
    bool m_isManagedReference;
    
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        m_attributeData = attribute as ClassSelectorAttribute;
        m_isManagedReference = property.propertyType == SerializedPropertyType.ManagedReference;
        
        // Infer the base type from the field if not explicitly provided
        Type baseType = m_attributeData?.Type;
        if (baseType == null)
        {
            property.GetFieldInfoAndStaticType(out Type staticType);
            baseType = staticType;
        }
        
        if (baseType == null)
        {
            return CreateErrorBox("Could not determine base type", $"property: {property.propertyPath}");
        }

        // Validate base type is suitable for ClassSelector
        string validationError = ValidateBaseType(baseType, m_isManagedReference);
        if (validationError != null)
        {
            return CreateErrorBox(validationError, $"Type: {baseType.Name}, Property: {property.propertyPath}");
        }
        return !m_isManagedReference ? CreateConcreteTypeUI(property, baseType) : CreatePolymorphicTypeUI(property, baseType);
    }

    /// <summary>
    /// Validates that the base type is appropriate for ClassSelector usage.
    /// </summary>
    static string ValidateBaseType(Type baseType, bool isManagedReference)
    {
        // Check 1: UnityEngine.Object types (MonoBehaviour, ScriptableObject, etc.)
        if (typeof(UnityEngine.Object).IsAssignableFrom(baseType))
        {
            if (isManagedReference)
            {
                return "[ClassSelector] cannot be used with UnityEngine.Object types on [SerializeReference] fields.\n" +
                       "Unity objects cannot be serialized as managed references.\n" +
                       "Remove [ClassSelector] or use a non-Unity object type.";
            }

            // For regular Unity object fields, ClassSelector doesn't add value
            return "[ClassSelector] is not needed for UnityEngine.Object types.\n" +
                   "Unity already provides object pickers for these types.\n" +
                   "Remove [ClassSelector] attribute.";
            
        }

        // Check 2: Interfaces with managed references
        if (baseType.IsInterface && isManagedReference)
        {
            // This is actually valid and useful - interfaces work with [SerializeReference]
            return null;
        }

        // Check 3: Value types (structs, primitives)
        if (baseType.IsValueType)
        {
            return "[ClassSelector] cannot be used with value types (structs).\n" +
                   "ClassSelector is designed for reference types only.\n" +
                   "Consider using a class instead of a struct.";
        }

        // Check 4: Generic types
        if (baseType.IsGenericTypeDefinition)
        {
            return "[ClassSelector] cannot be used with open generic types.\n" +
                   "Use a closed generic type (e.g., MyClass<int> instead of MyClass<T>).";
        }

        // Check 5: Static classes
        if (baseType.IsAbstract && baseType.IsSealed)
        {
            return "[ClassSelector] cannot be used with static classes.\n" +
                   "Static classes cannot be instantiated.";
        }

        // Check 6: Non-serializable types (if managed reference)
        if (isManagedReference && baseType.IsAbstract)
        {
            // Abstract types are fine with SerializeReference
            return null;
        }

        // Check 7: Types without any parameterless constructor (for concrete types)
        if (!baseType.IsAbstract && !baseType.IsInterface)
        {
            bool hasParameterlessConstructor = baseType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null) != null;

            if (!hasParameterlessConstructor)
            {
                return "[ClassSelector] requires types to have a parameterless constructor.\n" +
                       $"Type '{baseType.Name}' does not have a parameterless constructor.\n" +
                       $"Add a constructor: public {baseType.Name}() {{ }}";
            }
        }

        // Check 8: System types that shouldn't be instantiated
        if (baseType.Namespace == null || !baseType.Namespace.StartsWith("System") || baseType.IsInterface) return null;

        if (baseType != typeof(string) && 
            baseType != typeof(Uri) &&
            !baseType.IsGenericType)
        {
            return "[ClassSelector] should not be used with System types.\n" +
                   $"Type '{baseType.FullName}' is a framework type that may not serialize correctly.";
        }

        return null; // Validation passed
    }

    /// <summary>
    /// Creates a standardized error box for validation failures.
    /// </summary>
    static VisualElement CreateErrorBox(string message, string details)
    {
        VisualElement container = new() { style = { marginTop = 2, marginBottom = 2 } };
        
        HelpBox errorBox = new(message, HelpBoxMessageType.Error);
        container.Add(errorBox);

        if (string.IsNullOrEmpty(details)) return container;
        Label detailsLabel = new(details)
        {
            style = 
            { 
                fontSize = 10, 
                color = new StyleColor(Color.gray),
                marginLeft = 4,
                marginTop = 2,
                whiteSpace = WhiteSpace.Normal
            }
        };
        container.Add(detailsLabel);

        return container;
    }

    /// <summary>
    /// Creates the UI for concrete types (non-polymorphic, regular serialized fields).
    /// </summary>
    VisualElement CreateConcreteTypeUI(SerializedProperty property, Type baseType)
    {
        VisualElement root = new()
        {
            style = { marginTop = 2, marginBottom = 2 }
        };

        Foldout foldout = new()
        {
            text = property.displayName,
            value = property.isExpanded
        };
        
        foldout.RegisterValueChangedCallback(evt =>
        {
            property.isExpanded = evt.newValue;
            property.serializedObject.ApplyModifiedProperties();
        });

        VisualElement propertiesContainer = new()
        {
            style = { paddingLeft = 15, marginTop = 4 }
        };

        foldout.Add(propertiesContainer);
        root.Add(foldout);

        // Draw all fields of the concrete type
        PropertyDrawerCache.DrawUIForType(baseType, property, propertiesContainer, 
            excludeDrawerType: typeof(ClassSelectorPropertyDrawer));

        return root;
    }

    /// <summary>
    /// Creates the UI for polymorphic types (SerializeReference fields with type selection).
    /// </summary>
    VisualElement CreatePolymorphicTypeUI(SerializedProperty property, Type baseType)
    {
        VisualElement root = new()
        {
            style = { marginTop = 2, marginBottom = 2 }
        };

        Foldout foldout = new()
        {
            text = property.displayName,
            value = property.isExpanded
        };
        
        foldout.RegisterValueChangedCallback(evt =>
        {
            property.isExpanded = evt.newValue;
            property.serializedObject.ApplyModifiedProperties();
        });
        
        DropdownField dropdown = new()
        {
            name = "TypeSelectionDropdown",
            style = { marginBottom = 4, marginLeft = 0 }
        };

        VisualElement propertiesContainer = new()
        {
            name = "ObjectProperties",
            style = { paddingLeft = 15, marginTop = 4 }
        };

        foldout.Add(dropdown);
        foldout.Add(propertiesContainer);
        root.Add(foldout);
        
        // Get derived types and populate dropdown (filter out invalid types)
        List<Type> derivedTypes = PropertyDrawerCache.GetDerivedTypes(baseType, includeBaseType: !baseType.IsAbstract)
            .Where(IsTypeInstantiable)
            .ToList();
        
        if (derivedTypes.Count == 0)
        {
            VisualElement warningContainer = new();
            warningContainer.Add(new HelpBox(
                $"No valid instantiable types found that derive from {baseType.Name}.\n" +
                "Derived types must have parameterless constructors and cannot be Unity objects.",
                HelpBoxMessageType.Warning
            ));
            foldout.Add(warningContainer);
            return root;
        }
        
        Dictionary<string, Type> typesByName = derivedTypes.ToDictionary(t => t.Name, t => t);
        
        List<string> choices = new() { "None" };
        choices.AddRange(typesByName.Keys.OrderBy(name => name));
        dropdown.choices = choices;
        dropdown.SetValueWithoutNotify("None");
        
        // Handle type selection changes
        dropdown.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == "None")
            {
                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
                propertiesContainer.Clear();
                return;
            }
            
            if (!typesByName.TryGetValue(evt.newValue, out Type selectedType)) return;
            
            try
            {
                property.managedReferenceValue = Activator.CreateInstance(selectedType);
                property.serializedObject.ApplyModifiedProperties();
                    
                propertiesContainer.Clear();
                DrawManagedReferenceFields(property, propertiesContainer);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create instance of {selectedType.Name}: {ex.Message}");
                propertiesContainer.Clear();
                propertiesContainer.Add(new HelpBox(
                    $"Failed to instantiate {selectedType.Name}. Check that it has a parameterless constructor.",
                    HelpBoxMessageType.Error
                ));
            }
        });
        
        // Get the current value (handle backing fields for auto-properties)
        object currentValue = GetCurrentManagedReferenceValue(property);
        
        // Set the initial UI state
        if (currentValue == null)
        {
            dropdown.SetValueWithoutNotify("None");
            return root;
        }
        
        Type selectedType = currentValue.GetType();
        string typeName = selectedType.Name;

        if (!dropdown.choices.Contains(typeName)) return root;
        dropdown.SetValueWithoutNotify(typeName);
        DrawManagedReferenceFields(property, propertiesContainer);

        return root;
    }

    /// <summary>
    /// Checks if a type can be safely instantiated via ClassSelector.
    /// </summary>
    static bool IsTypeInstantiable(Type type)
    {
        // Skip Unity objects
        if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            return false;

        // Skip abstract types (shouldn't appear in the derived types list, but double-check)
        if (type.IsAbstract)
            return false;

        // Skip interfaces
        if (type.IsInterface)
            return false;

        // Skip value types
        if (type.IsValueType)
            return false;

        // Skip generic type definitions
        if (type.IsGenericTypeDefinition)
            return false;

        // Check for parameterless constructor
        bool hasParameterlessConstructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, Type.EmptyTypes, null) != null;

        return hasParameterlessConstructor;
    }

    /// <summary>
    /// Draws fields for a managed reference property.
    /// </summary>
    static void DrawManagedReferenceFields(SerializedProperty property, VisualElement container)
    {
        // Use manual iteration for managed references since GetChildren() doesn't work reliably
        SerializedProperty iterator = property.Copy();
        SerializedProperty endProperty = property.GetEndProperty();
        
        if (!iterator.NextVisible(true)) return;
        
        do
        {
            if (SerializedProperty.EqualContents(iterator, endProperty))
                break;

            // Create a copy of the iterator for this field
            SerializedProperty childProperty = iterator.Copy();
            
            // Try to use the custom drawer if available
            PropertyDrawer childDrawer = PropertyDrawerCache.CreateDrawerForProperty(
                childProperty, 
                excludeDrawerType: typeof(ClassSelectorPropertyDrawer));

            VisualElement customElement = childDrawer?.CreatePropertyGUI(childProperty);
            if (customElement != null)
            {
                container.Add(customElement);
                continue;
            }

            // Fallback to PropertyField
            container.Add(new PropertyField(childProperty));
            
        } while (iterator.NextVisible(false));
    }

    /// <summary>
    /// Gets the current managed reference value, handling auto-property backing fields.
    /// </summary>
    static object GetCurrentManagedReferenceValue(SerializedProperty property)
    {
        object currentValue = property.managedReferenceValue;

        if (currentValue != null) return currentValue;
        // Check for an auto-property backing field
        SerializedProperty backingField = property.serializedObject.FindProperty(
            $"<{property.name}>k__BackingField");

        if (backingField == null) return currentValue;
        currentValue = backingField.managedReferenceValue;
        if (currentValue != null)
        {
            property.managedReferenceValue = currentValue;
        }

        return currentValue;
    }
}
#endif