using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomNamespace.GenericDatatypes;
using UnityEngine;

namespace CustomNamespace.DependencyInjection
{
    [DefaultExecutionOrder(-1000)]
    public class Injector : Singleton<Injector>
    {
        const BindingFlags BindingFlags = System.Reflection.BindingFlags.Instance | 
                                         System.Reflection.BindingFlags.Public | 
                                         System.Reflection.BindingFlags.NonPublic;
        readonly Dictionary<Type, object> m_registry = new();

        protected override void Awake()
        {
            base.Awake();
            
            // Find all injectable providers and register them
            IEnumerable<IDependencyProvider> providers = FindMonoBehaviors().OfType<IDependencyProvider>();
            foreach (IDependencyProvider provider in providers)
            {
                RegisterProvider(provider);
            }
            
            // Find all injectable objects and inject their dependencies
            IEnumerable<MonoBehaviour> injectables = FindMonoBehaviors().Where(IsInjectable);
            foreach (MonoBehaviour injectable in injectables)
            {
                Inject(injectable);
            }
        }

        void RegisterProvider(IDependencyProvider provider)
        {
            MethodInfo[] methods = provider.GetType().GetMethods(BindingFlags);
            foreach (MethodInfo method in methods)
            {
                if (method.GetCustomAttribute<ProvideAttribute>() is null) continue;
                
                Type returnType = method.ReturnType;
                object providedInstance = method.Invoke(provider, null);
                
                if (providedInstance != null)
                {
                    m_registry.Add(returnType, providedInstance);
                }
                else
                {
                    throw new Exception($"Provider method {method.Name} returned null for type {returnType}");
                }
            }
        }

        void Inject(object instance)
        {
            Type type = instance.GetType();
            
            // Inject fields
            InjectFields(instance, type);
            
            // Inject properties
            InjectProperties(instance, type);
            
            // Inject methods
            InjectMethods(instance, type);
        }

        void InjectFields(object instance, Type type)
        {
            IEnumerable<FieldInfo> injectableFields = type.GetFields(BindingFlags)
                .Where(member => Attribute.IsDefined(member, typeof(InjectAttribute)));
            
            foreach (FieldInfo field in injectableFields)
            {
                Type fieldType = field.FieldType;
                object resolvedInstance = Resolve(fieldType);
                
                if (resolvedInstance == null)
                {
                    throw new Exception($"Failed to inject field '{field.Name}': Could not resolve instance of type {fieldType}");
                }
                
                field.SetValue(instance, resolvedInstance);
            }
        }

        void InjectProperties(object instance, Type type)
        {
            IEnumerable<PropertyInfo> injectableProperties = type.GetProperties(BindingFlags)
                .Where(member => Attribute.IsDefined(member, typeof(InjectAttribute)));
            
            foreach (PropertyInfo property in injectableProperties)
            {
                // Check if the property has a setter
                if (!property.CanWrite)
                {
                    throw new Exception($"Property '{property.Name}' has [Inject] attribute but no setter");
                }
                
                Type propertyType = property.PropertyType;
                object resolvedInstance = Resolve(propertyType);
                
                if (resolvedInstance == null)
                {
                    throw new Exception($"Failed to inject property '{property.Name}': Could not resolve instance of type {propertyType}");
                }
                
                property.SetValue(instance, resolvedInstance);
            }
        }

        void InjectMethods(object instance, Type type)
        {
            IEnumerable<MethodInfo> injectableMethods = type.GetMethods(BindingFlags)
                .Where(member => Attribute.IsDefined(member, typeof(InjectAttribute)));
            
            foreach (MethodInfo method in injectableMethods)
            {
                IEnumerable<Type> requiredParameters = method.GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .ToArray();
                    
                object[] resolvedInstances = requiredParameters.Select(Resolve).ToArray();
                
                if (resolvedInstances.Any(resolvedInstance => resolvedInstance == null))
                {
                    throw new Exception($"Failed to inject method '{method.Name}': Could not resolve all required parameters");
                }
                
                method.Invoke(instance, resolvedInstances);
            }
        }

        object Resolve(Type type)
        {
            m_registry.TryGetValue(type, out object resolvedInstance);
            return resolvedInstance;
        }

        static MonoBehaviour[] FindMonoBehaviors()
        {
            return FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.InstanceID);
        }

        static bool IsInjectable(MonoBehaviour monoBehaviour)
        {
            MemberInfo[] members = monoBehaviour.GetType().GetMembers(BindingFlags);
            return members.Any(member => member.GetCustomAttribute<InjectAttribute>() != null);
        }
    }
}