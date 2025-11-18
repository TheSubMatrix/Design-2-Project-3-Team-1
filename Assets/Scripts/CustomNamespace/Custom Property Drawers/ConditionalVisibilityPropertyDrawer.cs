#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using CustomNamespace.Extensions;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using CustomNamespace.Editor;

[CustomPropertyDrawer(typeof(ConditionalVisibilityAttribute))]
public class ConditionalVisibilityPropertyDrawer : PropertyDrawer
{
    VisualElement m_container;
    readonly Dictionary<string, SerializedProperty> m_conditionalProperties = new();

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        ConditionalVisibilityAttribute renderAttribute = (ConditionalVisibilityAttribute)attribute;
        
        // Create a container for the property
        m_container = new VisualElement();
        m_conditionalProperties.Clear();
        
        // Try to get the underlying property drawer for the field type
        PropertyDrawer underlyingDrawer = PropertyDrawerCache.CreateDrawerForProperty(property, typeof(ConditionalVisibilityPropertyDrawer));
        
        // Create the property field using the underlying drawer if available
        VisualElement propertyElement = underlyingDrawer?.CreatePropertyGUI(property) ?? new PropertyField(property);
        m_container.Add(propertyElement);

        // Parse the expression and find all conditional properties
        if (!ParseAndCacheProperties(property, renderAttribute.Expression))
        {
            return m_container; // Error already added
        }

        // Set initial visibility
        UpdateVisibility(property, renderAttribute);

        // Track changes to all conditional properties
        foreach (SerializedProperty propCopy in m_conditionalProperties.Select(kvp => kvp.Value.Copy()))
        {
            m_container.TrackPropertyValue(propCopy, _ => UpdateVisibility(property, renderAttribute));
        }

        return m_container;
    }

    bool ParseAndCacheProperties(SerializedProperty property, object[] expression)
    {
        if (expression == null || expression.Length == 0)
        {
            ShowError(property, "Expression is empty");
            return false;
        }

        // Find all field names in the expression and cache their SerializedProperties
        foreach (object t in expression)
        {
            if (t is not string str) continue;
            // Skip operators and parentheses
            if (str is "&&" or "||" or "(" or ")" or "==" or "!=" or "<" or ">" or "<=" or ">=")
            {
                continue;
            }

            // This should be a field name - cache it if we haven't already
            if (m_conditionalProperties.ContainsKey(str)) continue;
            SerializedProperty conditionalProperty = FindConditionalProperty(property, str);
            if (conditionalProperty == null)
            {
                ShowPropertyNotFoundError(property, str);
                return false;
            }
            m_conditionalProperties[str] = conditionalProperty;
        }

        return true;
    }

    void ShowError(SerializedProperty property, string message)
    {
        HelpBox errorBox = new(
            $"ConditionalVisibility Error on '{property.name}': {message}",
            HelpBoxMessageType.Error
        );
        m_container.Insert(0, errorBox);
    }

    void ShowPropertyNotFoundError(SerializedProperty property, string conditionalFieldName)
    {
        string propertyPath = property.propertyPath;
        int lastDot = propertyPath.LastIndexOf('.');
        string attemptedSiblingPath = lastDot == -1
            ? conditionalFieldName 
            : $"{propertyPath[..lastDot]}.{conditionalFieldName}";
        
        string backingFieldName = $"<{conditionalFieldName}>k__BackingField";
        string attemptedBackingFieldPath = lastDot == -1
            ? backingFieldName
            : $"{propertyPath[..lastDot]}.{backingFieldName}";

        HelpBox errorBox = new(
            $"Error: Conditional field '{conditionalFieldName}' not found for property '{property.name}'.\n" +
            $"Attempted paths:\n- Sibling: '{attemptedSiblingPath}'\n- Backing field: '{attemptedBackingFieldPath}'\n- Root: '{conditionalFieldName}'",
            HelpBoxMessageType.Error
        );
        m_container.Insert(0, errorBox);
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

    void UpdateVisibility(SerializedProperty property, ConditionalVisibilityAttribute renderAttribute)
    {
        bool shouldShow = EvaluateExpression(property, renderAttribute.Expression);
        
        // Hide the entire container including any nested drawers
        m_container.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        m_container.visible = shouldShow;
    }

    bool EvaluateExpression(SerializedProperty property, object[] expression)
    {
        try
        {
            int index = 0;
            return EvaluateOrExpression(property, expression, ref index);
        }
        catch (Exception ex)
        {
            Debug.LogError($"ConditionalVisibility: Error evaluating expression for '{property.name}': {ex.Message}");
            return false;
        }
    }

    // Recursive descent parser with proper precedence:
    // OR (||) - lowest precedence
    // AND (&&) - medium precedence
    // Comparison (==, !=, <, >, <=, >=) - highest precedence
    // Parentheses override precedence

    bool EvaluateOrExpression(SerializedProperty property, object[] expression, ref int index)
    {
        bool result = EvaluateAndExpression(property, expression, ref index);

        while (index < expression.Length && expression[index] is string op && op == "||")
        {
            index++; // consume ||
            bool right = EvaluateAndExpression(property, expression, ref index);
            result = result || right;
        }

        return result;
    }

    bool EvaluateAndExpression(SerializedProperty property, object[] expression, ref int index)
    {
        bool result = EvaluateComparisonExpression(property, expression, ref index);

        while (index < expression.Length && expression[index] is string op && op == "&&")
        {
            index++; // consume &&
            bool right = EvaluateComparisonExpression(property, expression, ref index);
            result = result && right;
        }

        return result;
    }

    bool EvaluateComparisonExpression(SerializedProperty property, object[] expression, ref int index)
    {
        // Check for parentheses first
        if (index < expression.Length && expression[index] is string s && s == "(")
        {
            index++; // consume (
            bool result = EvaluateOrExpression(property, expression, ref index);
            if (index < expression.Length && expression[index] is string s2 && s2 == ")")
            {
                index++; // consume )
            }
            return result;
        }

        // Otherwise, expect: fieldName [operator] value
        if (index >= expression.Length || expression[index] is not string fieldName)
        {
            throw new Exception($"Expected field name at position {index}");
        }

        index++; // consume field name

        // Default operator is == if not specified
        string comparisonOp = "==";
        
        // Check if next token is a comparison operator
        if (index < expression.Length && expression[index] is string potentialOp and ("==" or "!=" or "<" or ">" or "<=" or ">="))
        {
            comparisonOp = potentialOp;
            index++; // consume operator
        }

        // Get the value to compare against
        if (index >= expression.Length)
        {
            throw new Exception($"Expected value after field '{fieldName}'");
        }

        object expectedValue = expression[index];
        index++; // consume value

        // Get the actual value from the property
        if (!m_conditionalProperties.TryGetValue(fieldName, out SerializedProperty prop))
        {
            throw new Exception($"Property '{fieldName}' not found in cache");
        }

        object actualValue = prop.serializedObject.targetObject.GetFieldValue(prop.propertyPath);

        // Perform the comparison
        return CompareValues(actualValue, comparisonOp, expectedValue);
    }

    static bool CompareValues(object actual, string op, object expected)
    {
        // Handle null cases
        if (actual == null && expected == null)
            return op is "==" or "<=";
        if (actual == null || expected == null)
            return op == "!=";

        return op switch
        {
            "==" => Equals(actual, expected),
            "!=" => !Equals(actual, expected),
            "<" or ">" or "<=" or ">=" => CompareOrdered(actual, op, expected),
            _ => throw new Exception($"Unknown operator: {op}")
        };
    }

    static bool CompareOrdered(object actual, string op, object expected)
    {
        // Try to convert both to comparable values
        if (!TryConvertToComparable(actual, out IComparable actualComparable) ||
            !TryConvertToComparable(expected, out IComparable expectedComparable))
        {
            Debug.LogWarning($"Cannot perform ordered comparison on types {actual?.GetType()} and {expected?.GetType()}");
            return false;
        }

        try
        {
            int comparison = actualComparable.CompareTo(expectedComparable);

            return op switch
            {
                "<" => comparison < 0,
                ">" => comparison > 0,
                "<=" => comparison <= 0,
                ">=" => comparison >= 0,
                _ => false
            };
        }
        catch
        {
            Debug.LogWarning($"Comparison failed between {actual} and {expected}");
            return false;
        }
    }

    static bool TryConvertToComparable(object value, out IComparable comparable)
    {
        comparable = null;

        switch (value)
        {
            case null:
                return false;
            // If it's already comparable, use it directly
            case IComparable comp:
                comparable = comp;
                return true;
            default:
                // Try to convert to a comparable numeric type
                try
                {
                    comparable = Convert.ToDouble(value);
                    return true;
                }
                catch
                {
                    return false;
                }
        }
    }
}
#endif