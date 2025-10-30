using System;
using UnityEngine;

/// <summary>
/// Attribute to conditionally show/hide fields in the Inspector based on another field's value.
/// Supports any comparable value type.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ConditionalVisibilityAttribute : PropertyAttribute
{
    public string ConditionalFieldName { get; private set; }
    public object RequiredValue { get; private set; }

    /// <summary>
    /// Show this field only when the conditional field matches the required value.
    /// </summary>
    /// <param name="conditionalFieldName">Name of the field to check</param>
    /// <param name="requiredValue">Value that the conditional field must equal</param>
    public ConditionalVisibilityAttribute(string conditionalFieldName, object requiredValue)
    {
        ConditionalFieldName = conditionalFieldName;
        RequiredValue = requiredValue;
    }
}