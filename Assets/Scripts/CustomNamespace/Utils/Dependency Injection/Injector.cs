using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CustomNamespace.DependencyInjection {


    [DefaultExecutionOrder(-1000)]
    public class Injector : MonoBehaviour {
        const BindingFlags BindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        
        readonly Dictionary<Type, object> m_registry = new();

        void Awake() {
            MonoBehaviour[] monoBehaviours = FindMonoBehaviours();
            
            // Find all modules implementing IDependencyProvider and register the dependencies they provide
            IEnumerable<IDependencyProvider> providers = monoBehaviours.OfType<IDependencyProvider>();
            foreach (IDependencyProvider provider in providers) {
                Register(provider);
            }
            
            // Find all injectable objects and inject their dependencies
            IEnumerable<MonoBehaviour> injectables = monoBehaviours.Where(IsInjectable);
            foreach (MonoBehaviour injectable in injectables) {
                Inject(injectable);
            }
        }

        // Register an instance of a type outside the normal dependency injection process
        public void Register<T>(T instance) {
            m_registry[typeof(T)] = instance;
        }
        
        void Inject(object instance) 
        {
            Type type = instance.GetType();
            
            // Inject into fields
            IEnumerable<FieldInfo> injectableFields = type.GetFields(BindingFlags)
                .Where(member => Attribute.IsDefined(member, typeof(InjectAttribute)));

            foreach (FieldInfo injectableField in injectableFields) {
                object currentValue = injectableField.GetValue(instance);
                
                // Skip if field already has a non-null value
                if (currentValue != null) {
                    // Check if it's a Unity Object that's been destroyed
                    if (currentValue is UnityEngine.Object unityObj && unityObj == null) {
                        // Unity's "fake null" - proceed with injection
                    } else {
                        Debug.LogWarning($"[Injector] Field '{injectableField.Name}' of class '{type.Name}' is already set. Skipping injection.");
                        continue;
                    }
                }
                
                Type fieldType = injectableField.FieldType;
                object resolvedInstance = Resolve(fieldType);
                if (resolvedInstance == null) {
                    throw new Exception($"Failed to inject dependency into field '{injectableField.Name}' of class '{type.Name}'.");
                }
                
                injectableField.SetValue(instance, resolvedInstance);
            }
            
            // Inject into methods
            IEnumerable<MethodInfo> injectableMethods = type.GetMethods(BindingFlags)
                .Where(member => Attribute.IsDefined(member, typeof(InjectAttribute)));

            foreach (MethodInfo injectableMethod in injectableMethods) {
                Type[] requiredParameters = injectableMethod.GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .ToArray();
                object[] resolvedInstances = requiredParameters.Select(Resolve).ToArray();
                if (resolvedInstances.Any(resolvedInstance => resolvedInstance == null)) {
                    throw new Exception($"Failed to inject dependencies into method '{injectableMethod.Name}' of class '{type.Name}'.");
                }
                
                injectableMethod.Invoke(instance, resolvedInstances);
            }
            
            // Inject into properties
            IEnumerable<PropertyInfo> injectableProperties = type.GetProperties(BindingFlags)
                .Where(member => Attribute.IsDefined(member, typeof(InjectAttribute)));
            foreach (PropertyInfo injectableProperty in injectableProperties) {
                Type propertyType = injectableProperty.PropertyType;
                object resolvedInstance = Resolve(propertyType);
                if (resolvedInstance == null) {
                    throw new Exception($"Failed to inject dependency into property '{injectableProperty.Name}' of class '{type.Name}'.");
                }

                injectableProperty.SetValue(instance, resolvedInstance);
            }
            
            #if UNITY_EDITOR
            if (instance is UnityEngine.Object unityObjInstance) {
                UnityEditor.EditorUtility.SetDirty(unityObjInstance);
            }
            #endif
        }

        void Register(IDependencyProvider provider) {
            MethodInfo[] methods = provider.GetType().GetMethods(BindingFlags);

            foreach (MethodInfo method in methods) {
                if (!Attribute.IsDefined(method, typeof(ProvideAttribute))) continue;
                
                Type returnType = method.ReturnType;
                object providedInstance = method.Invoke(provider, null);
                if (providedInstance != null) {
                    m_registry.Add(returnType, providedInstance);
                } else {
                    throw new Exception($"Provider method '{method.Name}' in class '{provider.GetType().Name}' returned null when providing type '{returnType.Name}'.");
                }
            }
        }

        public static void ValidateDependencies() {
            MonoBehaviour[] monoBehaviours = FindMonoBehaviours();
            IEnumerable<IDependencyProvider> providers = monoBehaviours.OfType<IDependencyProvider>();
            HashSet<Type> providedDependencies = GetProvidedDependencies(providers);

            IEnumerable<string> invalidDependencies = monoBehaviours
                .SelectMany(mb => mb.GetType().GetFields(BindingFlags), (mb, field) => new {mb, field})
                .Where(t => Attribute.IsDefined(t.field, typeof(InjectAttribute)))
                .Where(t => !providedDependencies.Contains(t.field.FieldType) && t.field.GetValue(t.mb) == null)
                .Select(t => $"[Validation] {t.mb.GetType().Name} is missing dependency {t.field.FieldType.Name} on GameObject {t.mb.gameObject.name}");
            
            List<string> invalidDependencyList = invalidDependencies.ToList();
            
            if (!invalidDependencyList.Any()) {
                Debug.Log("[Validation] All dependencies are valid.");
            } else {
                Debug.LogError($"[Validation] {invalidDependencyList.Count} dependencies are invalid:");
                foreach (string invalidDependency in invalidDependencyList) {
                    Debug.LogError(invalidDependency);
                }
            }
        }

        static HashSet<Type> GetProvidedDependencies(IEnumerable<IDependencyProvider> providers) {
            HashSet<Type> providedDependencies = new HashSet<Type>();
            foreach (IDependencyProvider provider in providers) {
                MethodInfo[] methods = provider.GetType().GetMethods(BindingFlags);
                
                foreach (MethodInfo method in methods) {
                    if (!Attribute.IsDefined(method, typeof(ProvideAttribute))) continue;
                    
                    Type returnType = method.ReturnType;
                    providedDependencies.Add(returnType);
                }
            }

            return providedDependencies;
        }

        public static void ClearDependencies() {
            foreach (MonoBehaviour monoBehaviour in FindMonoBehaviours()) {
                Type type = monoBehaviour.GetType();
                IEnumerable<FieldInfo> injectableFields = type.GetFields(BindingFlags)
                    .Where(member => Attribute.IsDefined(member, typeof(InjectAttribute)));

                foreach (FieldInfo injectableField in injectableFields) {
                    injectableField.SetValue(monoBehaviour, null);
                }
            }
            
            Debug.Log("[Injector] All injectable fields cleared.");
        }

        object Resolve(Type type) {
            m_registry.TryGetValue(type, out object resolvedInstance);
            return resolvedInstance;
        }

        static MonoBehaviour[] FindMonoBehaviours() {
            return FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.InstanceID);
        }

        static bool IsInjectable(MonoBehaviour obj) {
            MemberInfo[] members = obj.GetType().GetMembers(BindingFlags);
            return members.Any(member => Attribute.IsDefined(member, typeof(InjectAttribute)));
        }
    }
}