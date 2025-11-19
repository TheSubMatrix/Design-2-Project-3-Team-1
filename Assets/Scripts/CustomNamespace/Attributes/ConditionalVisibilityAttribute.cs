using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ShowIfAttribute : PropertyAttribute
{
    public string ConditionName { get; private set; }
    public bool Invert { get; private set; }

    public ShowIfAttribute(string conditionName, bool invert = false)
    {
        ConditionName = conditionName;
        Invert = invert;
    }
}