#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomNamespace.Extensions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CustomNamespace.Editor
{
    /// <summary>
    /// Centralized caching system for PropertyDrawers, type hierarchies, and drawer metadata.
    /// Handles all reflection-based lookups with performance optimization.
    /// </summary>
    public static class PropertyDrawerCache
    {
        #region Cached Data Structures
        
        struct DrawerInfo
        {
            public Type DrawerType;
            public Type TargetType;
            public bool UseForChildren;
            public HashSet<string> HandledFields;
        }

        // Delegate for building UI for a specific type
        public delegate void BuildUIForType(SerializedProperty property, VisualElement container);

        static Dictionary<Type, List<Type>> s_derivedTypesCache;
        static Dictionary<Type, DrawerInfo> s_drawerByTargetTypeCache;
        static Dictionary<Type, DrawerInfo> s_drawerByDrawerTypeCache;
        static Dictionary<Type, BuildUIForType> s_UIBuilderCache;
        
        // Cache for field-specific drawer resolution (handles multi-attribute scenarios)
        static Dictionary<string, Type> s_fieldDrawerCache;
        
        #endregion

        #region Initialization

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            // Unsubscribe first to prevent duplicate subscriptions
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            RebuildCache();
        }

        static void OnCompilationFinished(object obj)
        {
            RebuildCache();
        }

        /// <summary>
        /// Rebuilds all caches by scanning assemblies for PropertyDrawers and type hierarchies.
        /// </summary>
        public static void RebuildCache()
        {
            s_derivedTypesCache = new Dictionary<Type, List<Type>>();
            s_drawerByTargetTypeCache = new Dictionary<Type, DrawerInfo>();
            s_drawerByDrawerTypeCache = new Dictionary<Type, DrawerInfo>();
            s_UIBuilderCache = new Dictionary<Type, BuildUIForType>();
            s_fieldDrawerCache = new Dictionary<string, Type>();

            IEnumerable<Type> allDrawers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(ass => ass.GetTypes())
                .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(PropertyDrawer)));

            foreach (Type drawerType in allDrawers)
            {
                foreach (CustomPropertyDrawer cpd in drawerType.GetCustomAttributes<CustomPropertyDrawer>(true))
                {
                    Type targetType = cpd.GetFieldValue<Type>("m_Type");
                    if (targetType == null) continue;

                    bool useForChildren = cpd.GetFieldValue<bool>("m_UseForChildren");
                    HashSet<string> handledFields = GetHandledFieldsForType(targetType);

                    DrawerInfo info = new DrawerInfo
                    {
                        DrawerType = drawerType,
                        TargetType = targetType,
                        UseForChildren = useForChildren,
                        HandledFields = handledFields
                    };

                    s_drawerByTargetTypeCache.TryAdd(targetType, info);
                    s_drawerByDrawerTypeCache.TryAdd(drawerType, info);
                }
            }
        }

        #endregion

        #region Type Hierarchy Queries

        /// <summary>
        /// Gets all non-abstract types that derive from or implement the specified base type.
        /// </summary>
        public static List<Type> GetDerivedTypes(Type baseType, bool includeBaseType = false)
        {
            if (baseType == null) return new List<Type>();
            if (s_derivedTypesCache == null) RebuildCache();

            if (s_derivedTypesCache.TryGetValue(baseType, out List<Type> types))
            {
                if (includeBaseType && !baseType.IsAbstract && !types.Contains(baseType))
                    return new List<Type>(types) { baseType };
                return types;
            }

            types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(ass => ass.GetTypes())
                .Where(t => t != baseType && !t.IsAbstract && baseType.IsAssignableFrom(t) 
                    && (baseType.IsInterface || !t.IsInterface))
                .ToList();

            if (includeBaseType && !baseType.IsAbstract)
                types.Add(baseType);

            s_derivedTypesCache[baseType] = types;
            return types;
        }

        #endregion

        #region Drawer Resolution

        public static Type GetDrawerType(Type targetType)
        {
            if (targetType == null) return null;
            if (s_drawerByTargetTypeCache == null) RebuildCache();

            if (s_drawerByTargetTypeCache.TryGetValue(targetType, out DrawerInfo info))
                return info.DrawerType;

            for (Type parentType = targetType.BaseType; parentType != null; parentType = parentType.BaseType)
            {
                if (s_drawerByTargetTypeCache.TryGetValue(parentType, out info) && info.UseForChildren)
                    return info.DrawerType;
            }

            foreach (Type interfaceType in targetType.GetInterfaces())
            {
                if (s_drawerByTargetTypeCache.TryGetValue(interfaceType, out info) && info.UseForChildren)
                    return info.DrawerType;
            }

            return null;
        }

        public static Type GetDrawerTargetType(Type drawerType)
        {
            if (drawerType == null) return null;
            if (s_drawerByDrawerTypeCache == null) RebuildCache();
            return s_drawerByDrawerTypeCache.TryGetValue(drawerType, out DrawerInfo info) ? info.TargetType : null;
        }

        public static HashSet<string> GetDrawerHandledFields(Type drawerType)
        {
            if (drawerType == null) return null;
            if (s_drawerByDrawerTypeCache == null) RebuildCache();
            return s_drawerByDrawerTypeCache.TryGetValue(drawerType, out DrawerInfo info) ? info.HandledFields : null;
        }

        #endregion

        #region Drawer Instantiation

        /// <summary>
        /// Creates a PropertyDrawer for the given property with caching for multi-attribute scenarios.
        /// </summary>
        public static PropertyDrawer CreateDrawerForProperty(
            SerializedProperty property, 
            Type excludeDrawerType = null,
            HashSet<Type> excludeDrawerTypes = null)
        {
            if (property == null) return null;

            FieldInfo fieldInfo = property.GetFieldInfoAndStaticType(out Type fieldType);
            if (fieldInfo == null) return null;

            // Generate cache key for this specific field (accounts for declaring type + field name)
            string cacheKey = $"{fieldInfo.DeclaringType?.FullName}.{fieldInfo.Name}";
            bool hasExclusions = excludeDrawerType != null || excludeDrawerTypes != null;

            // Use cached drawer type if no exclusions (exclusions prevent caching)
            if (!hasExclusions && s_fieldDrawerCache.TryGetValue(cacheKey, out Type cachedDrawerType))
            {
                if (cachedDrawerType == null) return null;
                
                // Check if it's an attribute-based drawer and get the attribute
                PropertyAttribute matchingAttr = fieldInfo.GetCustomAttributes<PropertyAttribute>(true)
                    .FirstOrDefault(attr => GetDrawerType(attr.GetType()) == cachedDrawerType);
                
                return CreateDrawerInstance(cachedDrawerType, fieldInfo, matchingAttr);
            }

            // Resolve drawer (prioritize attributes, then type-based)
            PropertyAttribute[] attributes = fieldInfo.GetCustomAttributes<PropertyAttribute>(true)
                .Reverse().ToArray();

            Type selectedDrawerType = null;
            PropertyAttribute selectedAttribute = null;

            // Priority 1: Attribute-based drawers
            foreach (PropertyAttribute attr in attributes)
            {
                Type drawerType = GetDrawerType(attr.GetType());
                if (drawerType != null && !IsDrawerExcluded(drawerType, excludeDrawerType, excludeDrawerTypes))
                {
                    selectedDrawerType = drawerType;
                    selectedAttribute = attr;
                    break;
                }
            }

            // Priority 2: Type-based drawer
            if (selectedDrawerType == null)
            {
                Type typeDrawer = GetDrawerType(fieldType);
                if (typeDrawer != null && !IsDrawerExcluded(typeDrawer, excludeDrawerType, excludeDrawerTypes))
                {
                    selectedDrawerType = typeDrawer;
                }
            }

            // Cache the result (even if null, to avoid repeated lookups)
            if (!hasExclusions)
                s_fieldDrawerCache[cacheKey] = selectedDrawerType;

            return selectedDrawerType != null 
                ? CreateDrawerInstance(selectedDrawerType, fieldInfo, selectedAttribute) 
                : null;
        }

        static bool IsDrawerExcluded(Type drawerType, Type excludeDrawerType, HashSet<Type> excludeDrawerTypes)
        {
            if (drawerType == excludeDrawerType) return true;
            return excludeDrawerTypes != null && excludeDrawerTypes.Contains(drawerType);
        }

        public static PropertyDrawer CreateDrawerInstance(Type drawerType, FieldInfo fieldInfo, PropertyAttribute attribute = null)
        {
            if (drawerType == null || !typeof(PropertyDrawer).IsAssignableFrom(drawerType))
                return null;

            try
            {
                PropertyDrawer drawer = (PropertyDrawer)Activator.CreateInstance(drawerType);
                drawer.SetFieldValue("m_FieldInfo", fieldInfo);
                if (attribute != null)
                    drawer.SetFieldValue("m_Attribute", attribute);
                return drawer;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"PropertyDrawerCache: Failed to create drawer instance for {drawerType.Name}: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Hybrid Drawer Support

        public static bool CreateHybridPropertyUI(
            SerializedProperty property, 
            VisualElement container,
            Type actualType,
            Type excludeDrawerType = null,
            HashSet<Type> excludeDrawerTypes = null)
        {
            if (property == null || container == null || actualType == null)
                return false;

            PropertyDrawer drawer = CreateDrawerForProperty(property, excludeDrawerType, excludeDrawerTypes);
            if (drawer == null) return false;

            Type drawerTargetType = GetDrawerTargetType(drawer.GetType());
            HashSet<string> fieldsHandledByDrawer = GetDrawerHandledFields(drawer.GetType());

            VisualElement customUI = drawer.CreatePropertyGUI(property);
            if (customUI != null)
                container.Add(customUI);

            if (drawerTargetType != null && drawerTargetType != actualType)
                AddAdditionalFields(property, container, fieldsHandledByDrawer, excludeDrawerTypes);

            return true;
        }

        static void AddAdditionalFields(
            SerializedProperty property,
            VisualElement container,
            HashSet<string> handledFields,
            HashSet<Type> excludeDrawerTypes = null)
        {
            VisualElement additionalContainer = new() { style = { marginTop = 8 } };
            bool hasFields = false;

            foreach (SerializedProperty child in property.GetChildren())
            {
                if (handledFields != null && handledFields.Contains(child.name))
                    continue;

                hasFields = true;
                PropertyDrawer childDrawer = CreateDrawerForProperty(child, null, excludeDrawerTypes);
                
                if (childDrawer != null)
                {
                    VisualElement customElement = childDrawer.CreatePropertyGUI(child);
                    if (customElement != null)
                    {
                        additionalContainer.Add(customElement);
                        continue;
                    }
                }
                
                additionalContainer.Add(new PropertyField(child));
            }

            if (hasFields)
                container.Add(additionalContainer);
        }

        /// <summary>
        /// Creates property fields for all children, respecting their custom drawers.
        /// </summary>
        public static void CreateDefaultPropertyFields(
            SerializedProperty property,
            VisualElement container,
            HashSet<Type> excludeDrawerTypes = null)
        {
            if (property == null || container == null)
                return;

            foreach (SerializedProperty child in property.GetChildren())
            {
                PropertyDrawer childDrawer = CreateDrawerForProperty(child, null, excludeDrawerTypes);
                
                if (childDrawer != null)
                {
                    VisualElement customElement = childDrawer.CreatePropertyGUI(child);
                    if (customElement != null)
                    {
                        container.Add(customElement);
                        continue;
                    }
                }
                
                container.Add(new PropertyField(child));
            }
        }

        #endregion
        
        #region UI Builder Cache System

        /// <summary>
        /// Draws UI for a specific type using cached builder delegates.
        /// </summary>
        public static void DrawUIForType(
            Type typeToDrawUIFor, 
            SerializedProperty property, 
            VisualElement container,
            Type excludeDrawerType = null,
            HashSet<Type> excludeDrawerTypes = null)
        {
            if (typeToDrawUIFor == null || property == null || container == null)
                return;

            // For excluded drawers, bypass cache and create one-off builder
            if (excludeDrawerType != null || excludeDrawerTypes != null)
            {
                DrawUIForTypeUncached(typeToDrawUIFor, property, container, excludeDrawerType, excludeDrawerTypes);
                return;
            }

            // Use cached builder for standard case
            BuildUIForType builderDelegate = GetOrCacheUIBuilder(typeToDrawUIFor);
            builderDelegate?.Invoke(property, container);
        }

        /// <summary>
        /// Draws UI without using the cache (for exclusion scenarios or one-off cases).
        /// </summary>
        static void DrawUIForTypeUncached(
            Type typeToDrawUIFor,
            SerializedProperty property, 
            VisualElement container,
            Type excludeDrawerType = null,
            HashSet<Type> excludeDrawerTypes = null)
        {
            bool usedCustomDrawer = CreateHybridPropertyUI(
                property, container, typeToDrawUIFor, 
                excludeDrawerType, excludeDrawerTypes);

            if (!usedCustomDrawer)
                CreateDefaultPropertyFields(property, container, excludeDrawerTypes);
        }

        static BuildUIForType GetOrCacheUIBuilder(Type typeToDrawUIFor)
        {
            if (typeToDrawUIFor == null) return null;

            s_UIBuilderCache ??= new Dictionary<Type, BuildUIForType>();

            if (s_UIBuilderCache.TryGetValue(typeToDrawUIFor, out BuildUIForType cached))
                return cached;
            
            // Check if there's a custom drawer for this type
            Type drawerType = GetDrawerType(typeToDrawUIFor);
            
            BuildUIForType builder;
            
            if (drawerType != null)
            {
                // Cache drawer metadata to avoid repeated lookups
                Type drawerTargetType = GetDrawerTargetType(drawerType);
                HashSet<string> handledFields = GetDrawerHandledFields(drawerType);
                
                builder = (prop, typeContainer) =>
                {
                    // Try to get FieldInfo for drawer instantiation
                    FieldInfo fieldInfo = prop.GetFieldInfoAndStaticType(out _);
                    if (fieldInfo == null)
                    {
                        // Fallback to default fields if we can't create the drawer
                        CreateDefaultPropertyFields(prop, typeContainer);
                        return;
                    }
                    
                    // Create drawer instance
                    PropertyDrawer drawer = CreateDrawerInstance(drawerType, fieldInfo);
                    if (drawer == null)
                    {
                        CreateDefaultPropertyFields(prop, typeContainer);
                        return;
                    }
                    
                    // Use the custom drawer
                    VisualElement customUI = drawer.CreatePropertyGUI(prop);
                    if (customUI != null)
                        typeContainer.Add(customUI);
                    
                    // Add additional fields if drawer doesn't handle all fields
                    if (drawerTargetType != null && drawerTargetType != typeToDrawUIFor)
                        AddAdditionalFields(prop, typeContainer, handledFields);
                };
            }
            else
            {
                // No custom drawer - use default field rendering
                builder = (prop, typeContainer) =>
                {
                    CreateDefaultPropertyFields(prop, typeContainer);
                };
            }
            
            s_UIBuilderCache[typeToDrawUIFor] = builder;
            return builder;
        }

        #endregion

        #region Helper Methods

        static HashSet<string> GetHandledFieldsForType(Type targetType)
        {
            HashSet<string> handledFields = new HashSet<string>();
            if (targetType == null) return handledFields;

            FieldInfo[] fields = targetType.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (FieldInfo field in fields)
            {
                if (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                    handledFields.Add(field.Name);
            }

            return handledFields;
        }

        public static bool DrawerHandlesField(Type drawerType, string fieldName)
        {
            HashSet<string> handledFields = GetDrawerHandledFields(drawerType);
            return handledFields != null && handledFields.Contains(fieldName);
        }

        #endregion
    }
}
#endif