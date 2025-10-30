#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomNamespace.Extensions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

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
            public HashSet<string> HandledFields; // Fields this drawer is responsible for
        }

        static Dictionary<Type, List<Type>> s_derivedTypesCache;
        static Dictionary<Type, DrawerInfo> s_drawerByTargetTypeCache;
        static Dictionary<Type, DrawerInfo> s_drawerByDrawerTypeCache;
        
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

            // Cache all property drawers
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
                    
                    // Build field set for this drawer
                    HashSet<string> handledFields = GetHandledFieldsForType(targetType);

                    DrawerInfo info = new DrawerInfo
                    {
                        DrawerType = drawerType,
                        TargetType = targetType,
                        UseForChildren = useForChildren,
                        HandledFields = handledFields
                    };

                    // Only add if not already present (first wins in case of conflicts)
                    s_drawerByTargetTypeCache.TryAdd(targetType, info);
                    
                    // Also cache by drawer type for reverse lookups
                    s_drawerByDrawerTypeCache.TryAdd(drawerType, info);
                }
            }
        }

        #endregion

        #region Type Hierarchy Queries

        /// <summary>
        /// Gets all non-abstract types that derive from or implement the specified base type.
        /// </summary>
        /// <param name="baseType">The base type or interface to find implementations of</param>
        /// <param name="includeBaseType">Whether to include the base type itself if it's not abstract</param>
        /// <returns>List of derived types</returns>
        public static List<Type> GetDerivedTypes(Type baseType, bool includeBaseType = false)
        {
            if (baseType == null)
                return new List<Type>();

            if (s_derivedTypesCache == null)
                RebuildCache();

            if (s_derivedTypesCache != null && s_derivedTypesCache.TryGetValue(baseType, out List<Type> types))
            {
                // If we need to include the base type, and it's not abstract, add it
                if (includeBaseType && !baseType.IsAbstract && !types.Contains(baseType))
                {
                    types = new List<Type>(types) { baseType };
                }
                return types;
            }

            // Compute derived types
            types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(ass => ass.GetTypes())
                .Where(t => t != baseType 
                    && !t.IsAbstract 
                    && baseType.IsAssignableFrom(t)
                    && (baseType.IsInterface || !t.IsInterface)) // Exclude interfaces unless base is interface
                .ToList();

            // Include the base type if it's not abstract and includeBaseType is true
            if (includeBaseType && !baseType.IsAbstract)
            {
                types.Add(baseType);
            }

            if (s_derivedTypesCache != null) s_derivedTypesCache[baseType] = types;
            return types;
        }

        #endregion

        #region Drawer Resolution

        /// <summary>
        /// Gets the PropertyDrawer type for a given target type (field type or attribute type).
        /// Follows Unity's drawer resolution rules: exact match first, then parent types/interfaces with useForChildren.
        /// </summary>
        /// <param name="targetType">The type to find a drawer for</param>
        /// <returns>The PropertyDrawer type, or null if none found</returns>
        public static Type GetDrawerType(Type targetType)
        {
            if (targetType == null)
                return null;

            if (s_drawerByTargetTypeCache == null)
                RebuildCache();

            // Direct exact match (the highest priority)
            if (s_drawerByTargetTypeCache != null && s_drawerByTargetTypeCache.TryGetValue(targetType, out DrawerInfo info))
            {
                return info.DrawerType;
            }

            // Check parent types (for class inheritance) - only if useForChildren is true
            for (Type parentType = targetType.BaseType; parentType != null; parentType = parentType.BaseType)
            {
                if (s_drawerByTargetTypeCache != null && s_drawerByTargetTypeCache.TryGetValue(parentType, out info) && info.UseForChildren)
                {
                    return info.DrawerType;
                }
            }

            // Check interfaces (for interface implementation) - only if useForChildren is true
            foreach (Type interfaceType in targetType.GetInterfaces())
            {
                if (s_drawerByTargetTypeCache != null && s_drawerByTargetTypeCache.TryGetValue(interfaceType, out info) && info.UseForChildren)
                {
                    return info.DrawerType;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the target type that a drawer was designed to handle.
        /// </summary>
        /// <param name="drawerType">The PropertyDrawer type</param>
        /// <returns>The target type (field type or attribute type), or null if not found</returns>
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

        /// <summary>
        /// Gets information about which fields a drawer handles.
        /// Useful for determining if additional fields need to be drawn when using inheritance.
        /// </summary>
        /// <param name="drawerType">The PropertyDrawer type</param>
        /// <returns>Set of field names the drawer handles, or null if drawer not found</returns>
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

        /// <summary>
        /// Gets information about which fields a target type's drawer handles.
        /// </summary>
        /// <param name="targetType">The type being drawn</param>
        /// <returns>Set of field names the drawer handles, or null if no drawer found</returns>
        public static HashSet<string> GetHandledFieldsForTargetType(Type targetType)
        {
            Type drawerType = GetDrawerType(targetType);
            return drawerType != null ? GetDrawerHandledFields(drawerType) : null;
        }
        #endregion

        #region Drawer Instantiation

        /// <summary>
        /// Creates and configures a PropertyDrawer instance for the specified property.
        /// </summary>
        /// <param name="property">The SerializedProperty to create a drawer for</param>
        /// <param name="excludeDrawerType">Optional drawer type to exclude (for wrapper drawers)</param>
        /// <returns>Configured PropertyDrawer instance, or null if none available</returns>
        public static PropertyDrawer CreateDrawerForProperty(SerializedProperty property, Type excludeDrawerType = null)
        {
            if (property == null)
                return null;

            FieldInfo fieldInfo = property.GetFieldInfoAndStaticType(out Type fieldType);
            if (fieldInfo == null)
                return null;

            // Priority 1: Check for PropertyAttribute-based drawers
            foreach (PropertyAttribute attr in fieldInfo.GetCustomAttributes<PropertyAttribute>(true))
            {
                Type attrType = attr.GetType();
                Type drawerType = GetDrawerType(attrType);

                if (drawerType != null && drawerType != excludeDrawerType)
                {
                    return CreateDrawerInstance(drawerType, fieldInfo, attr);
                }
            }

            // Priority 2: Check for type-based drawers
            Type typeDrawer = GetDrawerType(fieldType);
            if (typeDrawer != null && typeDrawer != excludeDrawerType)
            {
                return CreateDrawerInstance(typeDrawer, fieldInfo);
            }

            return null;
        }

        /// <summary>
        /// Creates and configures a PropertyDrawer instance.
        /// </summary>
        /// <param name="drawerType">The type of PropertyDrawer to instantiate</param>
        /// <param name="fieldInfo">The FieldInfo to assign to the drawer</param>
        /// <param name="attribute">Optional PropertyAttribute to assign to the drawer</param>
        /// <returns>Configured PropertyDrawer instance, or null if creation fails</returns>
        public static PropertyDrawer CreateDrawerInstance(Type drawerType, FieldInfo fieldInfo, PropertyAttribute attribute = null)
        {
            if (drawerType == null || !typeof(PropertyDrawer).IsAssignableFrom(drawerType))
                return null;

            try
            {
                PropertyDrawer drawer = (PropertyDrawer)Activator.CreateInstance(drawerType);

                // Set the fieldInfo on the drawer using reflection
                drawer.SetFieldValue("m_FieldInfo", fieldInfo);

                // Set the attribute if provided
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

        /// <summary>
        /// Creates a hybrid UI that combines a custom drawer with additional fields from derived types.
        /// This mimics Unity's behavior when a subclass extends a type that has a custom drawer.
        /// </summary>
        /// <param name="property">The SerializedProperty to create UI for</param>
        /// <param name="container">The VisualElement container to add UI to</param>
        /// <param name="actualType">The actual runtime type (this may be derived from the drawer's target type)</param>
        /// <param name="excludeDrawerType">Optional drawer type to exclude (for wrapper drawers)</param>
        /// <returns>True if a drawer was used, false if the drawer falls back to default fields</returns>
        public static bool CreateHybridPropertyUI(
            SerializedProperty property, 
            UnityEngine.UIElements.VisualElement container,
            Type actualType,
            Type excludeDrawerType = null)
        {
            if (property == null || container == null || actualType == null)
                return false;

            // Try to get a drawer for this property
            PropertyDrawer drawer = CreateDrawerForProperty(property, excludeDrawerType);

            if (drawer == null)
                return false;

            // Get the type the drawer was designed for
            Type drawerTargetType = GetDrawerTargetType(drawer.GetType());
            
            // Get fields handled by the drawer
            HashSet<string> fieldsHandledByDrawer = GetDrawerHandledFields(drawer.GetType());

            // Create the custom UI from the drawer
            UnityEngine.UIElements.VisualElement customUI = drawer.CreatePropertyGUI(property);
            if (customUI != null)
            {
                container.Add(customUI);
            }

            // If the actual type is different from (derived from) the drawer's target type,
            // add any additional fields that the drawer doesn't handle
            if (drawerTargetType != null && drawerTargetType != actualType)
            {
                AddAdditionalFields(property, container, fieldsHandledByDrawer);
            }

            return true;
        }

        /// <summary>
        /// Adds fields to the container that are not already handled by a drawer.
        /// Used when a derived type has additional fields beyond what the parent drawer handles.
        /// </summary>
        static void AddAdditionalFields(
            SerializedProperty property,
            UnityEngine.UIElements.VisualElement container,
            HashSet<string> handledFields)
        {
            bool hasAdditionalFields = false;
            UnityEngine.UIElements.VisualElement additionalFieldsContainer = new() 
            { 
                style = { marginTop = 8 } 
            };

            foreach (SerializedProperty child in property.GetChildren())
            {
                // Skip fields already handled by the drawer
                if (handledFields != null && handledFields.Contains(child.name))
                    continue;

                hasAdditionalFields = true;
                UnityEditor.UIElements.PropertyField field = new(child);
                additionalFieldsContainer.Add(field);
            }

            if (hasAdditionalFields)
            {
                container.Add(additionalFieldsContainer);
            }
        }

        /// <summary>
        /// Creates default property fields for all children of a SerializedProperty.
        /// This is the fallback when no custom drawer exists.
        /// </summary>
        /// <param name="property">The SerializedProperty to create fields for</param>
        /// <param name="container">The container to add fields to</param>
        public static void CreateDefaultPropertyFields(
            SerializedProperty property,
            UnityEngine.UIElements.VisualElement container)
        {
            if (property == null || container == null)
                return;

            foreach (SerializedProperty child in property.GetChildren())
            {
                UnityEditor.UIElements.PropertyField field = new(child);
                container.Add(field);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the set of field names that a drawer for the specified type would handle.
        /// </summary>
        static HashSet<string> GetHandledFieldsForType(Type targetType)
        {
            HashSet<string> handledFields = new HashSet<string>();

            if (targetType == null)
                return handledFields;

            FieldInfo[] fields = targetType.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (FieldInfo field in fields)
            {
                // Include public fields and fields with [SerializeField]
                if (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                {
                    handledFields.Add(field.Name);
                }
            }

            return handledFields;
        }

        /// <summary>
        /// Checks if a drawer handles a specific field.
        /// </summary>
        public static bool DrawerHandlesField(Type drawerType, string fieldName)
        {
            HashSet<string> handledFields = GetDrawerHandledFields(drawerType);
            return handledFields != null && handledFields.Contains(fieldName);
        }

        #endregion
    }
}
#endif