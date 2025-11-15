using System;
using UnityEngine;

/// <summary>
/// An attribute to enable a custom selector that allows the user to select any subclass
/// from a dropdown and render its serialized fields below the selector.
/// The base type is automatically inferred from the field type.
/// </summary>
public class ClassSelectorAttribute : PropertyAttribute
{
    public Type Type { get; set; }

    public ClassSelectorAttribute()
    {
    }
    
    public ClassSelectorAttribute(Type type)
    {
        this.Type = type;
    }
}