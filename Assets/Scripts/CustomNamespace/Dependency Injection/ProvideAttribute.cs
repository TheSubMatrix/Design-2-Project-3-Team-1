using System;

namespace CustomNamespace.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ProvideAttribute : Attribute
    {
        public ProvideAttribute(){}
    }
}