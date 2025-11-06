using System;
using UnityEngine;

namespace CustomNamespace.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public sealed class InjectAttribute : Attribute
    {
        public InjectAttribute(){}
    }
    public class Provider: MonoBehaviour, IDependencyProvider
    {
        
    }
}