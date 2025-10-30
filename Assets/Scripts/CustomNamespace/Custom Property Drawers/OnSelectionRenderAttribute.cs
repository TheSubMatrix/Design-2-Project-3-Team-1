using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
public class OnSelectionRenderAttribute : PropertyAttribute
{
    public string ConditionalFieldName { get; private set; }
    public object RequiredValue { get; private set; }
    public bool IsEnumComparison { get; private set; }

    /// <summary>
    /// Shows or hides the field in the Inspector based on the value of a specified boolean field.
    /// </summary>
    /// <param name="conditionalBoolFieldName">The name of the boolean field that controls visibility.</param>
    /// <param name="requiredBoolValue">The boolean value that must be matched for the field to be visible.</param>
    public OnSelectionRenderAttribute(string conditionalBoolFieldName, bool requiredBoolValue)
    {
        ConditionalFieldName = conditionalBoolFieldName;
        RequiredValue = requiredBoolValue;
        IsEnumComparison = false; // It's a bool comparison
    }

    /// <summary>
    /// Shows or hides the field in the Inspector based on the value of a specified enum field matching a specific value.
    /// </summary>
    /// <param name="conditionalEnumFieldName">The name of the enum field that controls visibility.</param>
    /// <param name="requiredEnumValue">The specific enum value that must be matched for the field to be visible. Cast this to object.</param>
    public OnSelectionRenderAttribute(string conditionalEnumFieldName, object requiredEnumValue)
    {
        // Important: requiredEnumValue should be an actual enum value (e.g., MyEnum.OptionA)
        // It's passed as object to allow generic enum types.
        ConditionalFieldName = conditionalEnumFieldName;
        RequiredValue = requiredEnumValue;
        IsEnumComparison = true; // It's an enum comparison
    }
}


