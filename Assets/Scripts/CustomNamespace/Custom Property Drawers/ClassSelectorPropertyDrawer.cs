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
    static readonly Dictionary<Type, BuildUIForType> s_UIBuilderCache = new();
    ClassSelectorAttribute m_attributeData;
    
    delegate void BuildUIForType(SerializedProperty property, VisualElement container);
    
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        m_attributeData = attribute as ClassSelectorAttribute;
        
        // Infer the base type from the field if not explicitly provided
        Type baseType = m_attributeData?.Type;
        if (baseType == null)
        {
            property.GetFieldInfoAndStaticType(out Type staticType);
            baseType = staticType;
        }
        
        if (baseType == null)
        {
            Debug.LogError($"Could not determine base type for property {property.propertyPath}");
            return new Label("Error: Could not determine base type");
        }
        
        VisualElement root = new()
        {
            style =
            {
                marginTop = 2,
                marginBottom = 2
            }
        };

        // Create foldout for collapsing
        Foldout foldout = new()
        {
            text = property.displayName,
            value = property.isExpanded
        };
        
        // Save the expanded state
        foldout.RegisterValueChangedCallback(evt =>
        {
            property.isExpanded = evt.newValue;
            property.serializedObject.ApplyModifiedProperties();
        });
        
        DropdownField dropdown = new()
        {
            name = "TypeSelectionDropdown",
            style =
            {
                marginBottom = 4,
                marginLeft = 0
            }
        };

        // Container for object properties
        VisualElement propertiesContainer = new()
        {
            name = "ObjectProperties",
            style =
            {
                paddingLeft = 15,
                marginTop = 4
            }
        };

        foldout.Add(dropdown);
        foldout.Add(propertiesContainer);
        root.Add(foldout);
        
        // Get derived types and populate dropdown
        List<Type> derivedTypes = PropertyDrawerCache.GetDerivedTypes(baseType, includeBaseType: true);
        Dictionary<string, Type> typesByName = derivedTypes.ToDictionary(t => t.Name, t => t);
        
        List<string> choices = new() { "None" };
        choices.AddRange(typesByName.Keys.OrderBy(name => name));
        dropdown.choices = choices;
        dropdown.SetValueWithoutNotify("None");
        
        // Handle type selection changes
        Type selectedType = null;
        dropdown.RegisterValueChangedCallback(evt =>
        {
            // Handle "None" selection
            if (evt.newValue == "None")
            {
                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
                propertiesContainer.Clear();
                return;
            }
            
            if (!typesByName.TryGetValue(evt.newValue, out selectedType)) return;
            property.managedReferenceValue = Activator.CreateInstance(selectedType);
            property.serializedObject.ApplyModifiedProperties();
                
            propertiesContainer.Clear();
            DrawUIForType(selectedType, property, propertiesContainer);
        });
        
        // Handle [field: SerializeField] by checking the backing field
        object currentValue = property.managedReferenceValue;
        if (currentValue == null)
        {
            SerializedProperty backingField = property.serializedObject.FindProperty($"<{property.name}>k__BackingField");
            if (backingField != null)
            {
                currentValue = backingField.managedReferenceValue;
                if (currentValue != null)
                {
                    property.managedReferenceValue = currentValue;
                }
            }
        }
        
        // Set the initial value and draw UI
        if (currentValue == null)
        {
            dropdown.SetValueWithoutNotify("None");
            return root;
        }
        
        selectedType = currentValue.GetType();
        int index = dropdown.choices.IndexOf(selectedType.Name);
        if (index < 0) return root;
        dropdown.SetValueWithoutNotify(dropdown.choices[index]);
        DrawUIForType(selectedType, property, propertiesContainer);

        return root;
    }

    static void DrawUIForType(Type typeToDrawUIFor, SerializedProperty property, VisualElement container)
    {
        BuildUIForType builderDelegate = GetOrCacheUIBuilder(typeToDrawUIFor);
        builderDelegate?.Invoke(property, container);
    }

    static BuildUIForType GetOrCacheUIBuilder(Type typeToDrawUIFor)
    {
        if (typeToDrawUIFor == null)
            return null;
    
        if (s_UIBuilderCache.TryGetValue(typeToDrawUIFor, out BuildUIForType cached))
            return cached;
        
        // Create and cache the builder
        BuildUIForType builder = CreateBuilderForType(typeToDrawUIFor);
        s_UIBuilderCache[typeToDrawUIFor] = builder;
        return builder;
    }

    static BuildUIForType CreateBuilderForType(Type typeToDrawUIFor)
    {
        return (prop, typeContainer) =>
        {
            // Use the unified hybrid drawer system from the cache
            bool usedCustomDrawer = PropertyDrawerCache.CreateHybridPropertyUI(
                prop, 
                typeContainer, 
                typeToDrawUIFor, 
                typeof(ClassSelectorPropertyDrawer));

            // Fallback: Draw all properties using default property fields
            if (!usedCustomDrawer)
            {
                PropertyDrawerCache.CreateDefaultPropertyFields(prop, typeContainer);
            }
        };
    }
}
#endif