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
        
        #endregion

        #region Initialization

        [InitializeOnLoadMethod]
        static void Initialize()
        {
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

            IEnumerable<Type> allDrawers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(ass => ass.GetTypes())
                .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(PropertyDrawer)));

            foreach (Type drawerType in allDrawers)
            {
                foreach (CustomPropertyDrawer cpd in drawerType.GetCustomAttributes<CustomPropertyDrawer>(true))
                {
                    Type targetType = cpd.GetFieldValue<Type>("m_Type");
                    if (targetType == null)
                        continue;

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
            if (baseType == null)
                return new List<Type>();

            if (s_derivedTypesCache == null)
                RebuildCache();

            if (s_derivedTypesCache != null && s_derivedTypesCache.TryGetValue(baseType, out List<Type> types))
            {
                if (includeBaseType && !baseType.IsAbstract && !types.Contains(baseType))
                {
                    types = new List<Type>(types) { baseType };
                }
                return types;
            }

            types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(ass => ass.GetTypes())
                .Where(t => t != baseType 
                    && !t.IsAbstract 
                    && baseType.IsAssignableFrom(t)
                    && (baseType.IsInterface || !t.IsInterface))
                .ToList();

            if (includeBaseType && !baseType.IsAbstract)
            {
                types.Add(baseType);
            }

            if (s_derivedTypesCache != null) s_derivedTypesCache[baseType] = types;
            return types;
        }

        #endregion

        #region Drawer Resolution

        public static Type GetDrawerType(Type targetType)
        {
            if (targetType == null)
                return null;

            if (s_drawerByTargetTypeCache == null)
                RebuildCache();

            if (s_drawerByTargetTypeCache != null && s_drawerByTargetTypeCache.TryGetValue(targetType, out DrawerInfo info))
            {
                return info.DrawerType;
            }

            for (Type parentType = targetType.BaseType; parentType != null; parentType = parentType.BaseType)
            {
                if (s_drawerByTargetTypeCache != null && s_drawerByTargetTypeCache.TryGetValue(parentType, out info) && info.UseForChildren)
                {
                    return info.DrawerType;
                }
            }

            foreach (Type interfaceType in targetType.GetInterfaces())
            {
                if (s_drawerByTargetTypeCache != null && s_drawerByTargetTypeCache.TryGetValue(interfaceType, out info) && info.UseForChildren)
                {
                    return info.DrawerType;
                }
            }

            return null;
        }

        public static Type GetDrawerTargetType(Type drawerType)
        {
            if (drawerType == null)
                return null;

            if (s_drawerByDrawerTypeCache == null)
                RebuildCache();

            return s_drawerByDrawerTypeCache != null && s_drawerByDrawerTypeCache.TryGetValue(drawerType, out DrawerInfo info) 
                ? info.TargetType 
                : null;
        }

        public static HashSet<string> GetDrawerHandledFields(Type drawerType)
        {
            if (drawerType == null)
                return null;

            if (s_drawerByDrawerTypeCache == null)
                RebuildCache();

            return s_drawerByDrawerTypeCache != null && s_drawerByDrawerTypeCache.TryGetValue(drawerType, out DrawerInfo info) 
                ? info.HandledFields 
                : null;
        }

        public static HashSet<string> GetHandledFieldsForTargetType(Type targetType)
        {
            Type drawerType = GetDrawerType(targetType);
            return drawerType != null ? GetDrawerHandledFields(drawerType) : null;
        }
        #endregion

        #region Drawer Instantiation

        /// <summary>
        /// Creates a PropertyDrawer for the given property, considering both attribute-based
        /// and type-based drawers. Allows excluding specific drawer types.
        /// </summary>
        public static PropertyDrawer CreateDrawerForProperty(
            SerializedProperty property, 
            Type excludeDrawerType = null,
            HashSet<Type> excludeDrawerTypes = null)
        {
            if (property == null)
                return null;

            FieldInfo fieldInfo = property.GetFieldInfoAndStaticType(out Type fieldType);
            if (fieldInfo == null)
                return null;

            // Priority 1: Check PropertyAttribute-based drawers
            // Process in reverse order so the last attribute (top in code) takes precedence
            PropertyAttribute[] attributes = fieldInfo.GetCustomAttributes<PropertyAttribute>(true)
                .Reverse()
                .ToArray();

            foreach (PropertyAttribute attr in attributes)
            {
                Type attrType = attr.GetType();
                Type drawerType = GetDrawerType(attrType);

                if (drawerType != null && !IsDrawerExcluded(drawerType, excludeDrawerType, excludeDrawerTypes))
                {
                    return CreateDrawerInstance(drawerType, fieldInfo, attr);
                }
            }

            // Priority 2: Check type-based drawer
            Type typeDrawer = GetDrawerType(fieldType);
            if (typeDrawer != null && !IsDrawerExcluded(typeDrawer, excludeDrawerType, excludeDrawerTypes))
            {
                return CreateDrawerInstance(typeDrawer, fieldInfo);
            }

            return null;
        }

        /// <summary>
        /// Gets all PropertyDrawers that could apply to this property (both attribute and type-based).
        /// Useful for scenarios where you need to chain or inspect multiple drawers.
        /// </summary>
        public static List<PropertyDrawer> GetAllDrawersForProperty(
            SerializedProperty property,
            HashSet<Type> excludeDrawerTypes = null)
        {
            List<PropertyDrawer> drawers = new List<PropertyDrawer>();
            
            if (property == null)
                return drawers;

            FieldInfo fieldInfo = property.GetFieldInfoAndStaticType(out Type fieldType);
            if (fieldInfo == null)
                return drawers;

            // Collect all attribute-based drawers
            foreach (PropertyAttribute attr in fieldInfo.GetCustomAttributes<PropertyAttribute>(true))
            {
                Type attrType = attr.GetType();
                Type drawerType = GetDrawerType(attrType);

                if (drawerType == null || IsDrawerExcluded(drawerType, null, excludeDrawerTypes)) continue;
                PropertyDrawer drawer = CreateDrawerInstance(drawerType, fieldInfo, attr);
                if (drawer != null)
                    drawers.Add(drawer);
            }

            // Add type-based drawer if it exists
            Type typeDrawer = GetDrawerType(fieldType);
            if (typeDrawer != null && !IsDrawerExcluded(typeDrawer, null, excludeDrawerTypes))
            {
                PropertyDrawer drawer = CreateDrawerInstance(typeDrawer, fieldInfo);
                if (drawer != null)
                    drawers.Add(drawer);
            }

            return drawers;
        }

        static bool IsDrawerExcluded(Type drawerType, Type excludeDrawerType, HashSet<Type> excludeDrawerTypes)
        {
            if (drawerType == excludeDrawerType)
                return true;

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
                {
                    drawer.SetFieldValue("m_Attribute", attribute);
                }

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

            if (drawer == null)
                return false;

            Type drawerTargetType = GetDrawerTargetType(drawer.GetType());
            HashSet<string> fieldsHandledByDrawer = GetDrawerHandledFields(drawer.GetType());

            VisualElement customUI = drawer.CreatePropertyGUI(property);
            if (customUI != null)
            {
                container.Add(customUI);
            }

            if (drawerTargetType != null && drawerTargetType != actualType)
            {
                AddAdditionalFields(property, container, fieldsHandledByDrawer);
            }

            return true;
        }

        static void AddAdditionalFields(
            SerializedProperty property,
            VisualElement container,
            HashSet<string> handledFields)
        {
            bool hasAdditionalFields = false;
            VisualElement additionalFieldsContainer = new() 
            { 
                style = { marginTop = 8 } 
            };

            foreach (SerializedProperty child in property.GetChildren())
            {
                if (handledFields != null && handledFields.Contains(child.name))
                    continue;

                hasAdditionalFields = true;
                PropertyField field = new(child);
                additionalFieldsContainer.Add(field);
            }

            if (hasAdditionalFields)
            {
                container.Add(additionalFieldsContainer);
            }
        }

        public static void CreateDefaultPropertyFields(
            SerializedProperty property,
            VisualElement container)
        {
            if (property == null || container == null)
                return;

            foreach (SerializedProperty child in property.GetChildren())
            {
                PropertyField field = new(child);
                container.Add(field);
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

            // For excluded drawers, we can't use cached builders since they're type-specific
            // Create a one-off builder instead
            if (excludeDrawerType != null || excludeDrawerTypes != null)
            {
                bool usedCustomDrawer = CreateHybridPropertyUI(
                    property, 
                    container, 
                    typeToDrawUIFor, 
                    excludeDrawerType,
                    excludeDrawerTypes);

                if (!usedCustomDrawer)
                {
                    CreateDefaultPropertyFields(property, container);
                }
                return;
            }

            // Use cached builder for standard case (no exclusions)
            BuildUIForType builderDelegate = GetOrCacheUIBuilder(typeToDrawUIFor);
            builderDelegate?.Invoke(property, container);
        }

        static BuildUIForType GetOrCacheUIBuilder(Type typeToDrawUIFor)
        {
            if (typeToDrawUIFor == null)
                return null;

            s_UIBuilderCache ??= new Dictionary<Type, BuildUIForType>();

            if (s_UIBuilderCache.TryGetValue(typeToDrawUIFor, out BuildUIForType cached))
                return cached;
            
            // Create and cache the builder
            BuildUIForType builder = CreateBuilderForType(typeToDrawUIFor);
            s_UIBuilderCache[typeToDrawUIFor] = builder;
            return builder;
        }

        static BuildUIForType CreateBuilderForType(Type typeToDrawUIFor)
        {
            return (prop, typeContainer) =>
            {
                // Use the unified hybrid drawer system from the cache
                bool usedCustomDrawer = CreateHybridPropertyUI(
                    prop, 
                    typeContainer, 
                    typeToDrawUIFor);

                // Fallback: Draw all properties using default property fields
                if (!usedCustomDrawer)
                {
                    CreateDefaultPropertyFields(prop, typeContainer);
                }
            };
        }

        #endregion

        #region Helper Methods

        static HashSet<string> GetHandledFieldsForType(Type targetType)
        {
            HashSet<string> handledFields = new HashSet<string>();

            if (targetType == null)
                return handledFields;

            FieldInfo[] fields = targetType.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (FieldInfo field in fields)
            {
                if (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                {
                    handledFields.Add(field.Name);
                }
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