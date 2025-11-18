using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ConditionalVisibilityAttribute : PropertyAttribute
{
    public object[] Expression { get; }

    // Expression-based constructor supporting full conditional logic
    public ConditionalVisibilityAttribute(params object[] expression)
    {
        Expression = expression;
    }
}