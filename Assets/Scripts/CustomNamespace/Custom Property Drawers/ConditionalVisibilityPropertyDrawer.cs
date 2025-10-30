using UnityEngine;
using System;
using CustomNamespace.Editor;
using CustomNamespace.Extensions;


#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

[CustomPropertyDrawer(typeof(ConditionalVisibilityAttribute))]
public class ConditionalVisibilityPropertyDrawer : PropertyDrawer
{
    VisualElement container;

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        ConditionalVisibilityAttribute renderAttribute = (ConditionalVisibilityAttribute)attribute;
        
        // Create a container for the property
        container = new VisualElement();
        
        // Try to get the underlying property drawer for the field type
        PropertyDrawer underlyingDrawer = PropertyDrawerCache.CreateDrawerForProperty(property, typeof(ConditionalVisibilityPropertyDrawer));
        
        // Create the property field using the underlying drawer if available
        VisualElement propertyElement = underlyingDrawer?.CreatePropertyGUI(property) ?? new PropertyField(property);
        container.Add(propertyElement);

        // Find the conditional property using the new relative logic
        SerializedProperty conditionalProperty = FindConditionalProperty(property, renderAttribute.ConditionalFieldName);

        if (conditionalProperty == null)
        {
            // Construct the attempted sibling path for the error message
            string propertyPath = property.propertyPath;
            int lastDot = propertyPath.LastIndexOf('.');
            string attemptedSiblingPath = lastDot == -1
                ? renderAttribute.ConditionalFieldName 
                : $"{propertyPath[..lastDot]}.{renderAttribute.ConditionalFieldName}";
            
            string backingFieldName = $"<{renderAttribute.ConditionalFieldName}>k__BackingField";
            string attemptedBackingFieldPath = lastDot == -1
                ? backingFieldName
                : $"{propertyPath[..lastDot]}.{backingFieldName}";

            // Show a more detailed error
            HelpBox errorBox = new(
                $"Error: Conditional field '{renderAttribute.ConditionalFieldName}' not found for property '{property.name}'.\n" +
                $"Attempted paths:\n- Sibling: '{attemptedSiblingPath}'\n- Backing field: '{attemptedBackingFieldPath}'\n- Root: '{renderAttribute.ConditionalFieldName}'",
                HelpBoxMessageType.Error
            );
            container.Insert(0, errorBox);
            return container;
        }

        // Set initial visibility
        UpdateVisibility(conditionalProperty, renderAttribute);

        // Track changes to the conditional property
        container.TrackPropertyValue(conditionalProperty, _ => UpdateVisibility(conditionalProperty, renderAttribute));

        return container;
    }

    /// <summary>
    /// Finds the conditional property, trying as a sibling first, then as a root property.
    /// Also handles [field: SerializeField] auto-properties by checking for backing field names.
    /// </summary>
    static SerializedProperty FindConditionalProperty(SerializedProperty property, string conditionalFieldName)
    {
        // Try to resolve relative to the same object context (nested class, managed reference, etc.)
        string propertyPath = property.propertyPath;
        int lastDot = propertyPath.LastIndexOf('.');

        // Build the sibling path (e.g., parent.childField)
        string conditionalPropertyPath = lastDot >= 0
            ? $"{propertyPath[..lastDot]}.{conditionalFieldName}"
            : conditionalFieldName;

        SerializedProperty conditionalProperty = property.serializedObject.FindProperty(conditionalPropertyPath);

        if (conditionalProperty != null)
            return conditionalProperty;

        // Try auto-property backing field name format: <PropertyName>k__BackingField
        string backingFieldName = $"<{conditionalFieldName}>k__BackingField";
        string backingFieldPath = lastDot >= 0
            ? $"{propertyPath[..lastDot]}.{backingFieldName}"
            : backingFieldName;

        conditionalProperty = property.serializedObject.FindProperty(backingFieldPath);

        if (conditionalProperty != null)
            return conditionalProperty;

        // Fallback: search inside the same parent object explicitly
        SerializedProperty parent = property.Copy();
        int depth = property.depth;
        while (parent.depth >= depth && parent.propertyPath != property.propertyPath)
            parent.NextVisible(false);

        SerializedProperty test = parent.Copy();
        if (!test.NextVisible(true)) return null;
        do
        {
            if (test.depth <= depth && (test.name == conditionalFieldName || test.name == backingFieldName))
                return test.Copy();
        } while (test.NextVisible(false));

        return null;
    }


    void UpdateVisibility(SerializedProperty conditionalProperty, ConditionalVisibilityAttribute renderAttribute)
    {
        bool shouldShow = EvaluateCondition(conditionalProperty, renderAttribute);
        
        // Hide the entire container including any nested drawers (like RequiredField icons)
        container.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        
        // Additionally, set visibility to prevent space allocation
        container.visible = shouldShow;
    }

    static bool EvaluateCondition(SerializedProperty conditionalProperty, ConditionalVisibilityAttribute renderAttribute)
    {
        if (conditionalProperty == null || renderAttribute.RequiredValue == null)
        {
            return false;
        }

        try
        {
            object actualValue = conditionalProperty.serializedObject.targetObject.GetFieldValue(conditionalProperty.propertyPath);
            return Equals(actualValue, renderAttribute.RequiredValue);
        }
        catch (Exception ex)
        {
            Debug.LogError($"ConditionalVisibilityAttribute: Error evaluating condition for property '{conditionalProperty.name}': {ex.Message}");
            return false;
        }
    }
}
#endif