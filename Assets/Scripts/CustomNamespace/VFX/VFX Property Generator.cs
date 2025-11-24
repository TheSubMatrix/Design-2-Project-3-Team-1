// VFXPropertyGenerator.cs
// Place this in an Editor folder
#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace VFXPropertyGeneration
{
    public static class VFXPropertyGenerator
    {
        /// <summary>
        /// Defines a property type to be generated.
        /// </summary>
        readonly struct PropertyTypeDefinition : IEquatable<PropertyTypeDefinition>
        {
            public readonly string ClassName;    // e.g., "Float"
            public readonly string ValueType;    // e.g., "float"
            public readonly string VFXSetMethod; // e.g., "SetFloat"
            public readonly bool IsValueType;    // Is this a struct/value type?

            public PropertyTypeDefinition(string className, string valueType, string vfxSetMethod, bool isValueType)
            {
                ClassName = className;
                ValueType = valueType;
                VFXSetMethod = vfxSetMethod;
                IsValueType = isValueType;
            }

            // Implement Equals and GetHashCode for .Distinct() to work correctly
            public override bool Equals(object obj)
            {
                return obj is PropertyTypeDefinition d &&
                       ClassName == d.ClassName &&
                       ValueType == d.ValueType &&
                       VFXSetMethod == d.VFXSetMethod;
            }

            public override int GetHashCode()
            {
                return (ClassName, ValueType, VFXSetMethod).GetHashCode();
            }

            public bool Equals(PropertyTypeDefinition other)
            {
                return ClassName == other.ClassName && ValueType == other.ValueType && VFXSetMethod == other.VFXSetMethod && IsValueType == other.IsValueType;
            }
        }

        // Helper dictionary to map C# types to their keyword strings (e.g., System.Single -> "float")
        // This is a deliberate choice for generating more readable code (float vs. Single).
        static readonly Dictionary<Type, string> s_typeKeywords = new Dictionary<Type, string>
        {
            { typeof(float), "float" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(bool), "bool" }
        };

        /// <summary>
        /// Gets the correct C# string representation for a type.
        /// </summary>
        static string GetTypeString(Type t)
        {
            return s_typeKeywords.GetValueOrDefault(t, t.Name);
        }

        /// <summary>
        /// Uses reflection to find all valid Set... methods on the VisualEffect class.
        /// </summary>
        static List<PropertyTypeDefinition> GetVFXPropertyDefinitions()
        {
            List<PropertyTypeDefinition> definitions = new List<PropertyTypeDefinition>();
            Type vfxType = typeof(VisualEffect);

            // Find all public instance methods that...
            IEnumerable<MethodInfo> methods = vfxType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.StartsWith("Set") &&      // ...start with "Set"
                            m.GetParameters().Length == 2 && // ...have exactly 2 parameters
                            m.GetParameters()[0].ParameterType == typeof(int) && // ...first param is int (NameID)
                            m.GetParameters()[1].ParameterType != typeof(VFXEventAttribute)); // ...exclude SetEventAttribute

            foreach (MethodInfo method in methods)
            {
                string vfxSetMethod = method.Name;
                Type valueType = method.GetParameters()[1].ParameterType;
                string valueTypeName = GetTypeString(valueType);
                
                // "SetFloat" -> "Float"
                string className = vfxSetMethod[3..]; 

                // Add the definition, dynamically checking if it's a value type
                definitions.Add(new PropertyTypeDefinition(className, valueTypeName, vfxSetMethod, valueType.IsValueType));

                // Handle the Color special case
                // If we find SetVector4, also add an alias for Color
                if (vfxSetMethod == "SetVector4")
                {
                    // typeof(Color).IsValueType will correctly return true
                    definitions.Add(new PropertyTypeDefinition("Color", "Color", "SetVector4", typeof(Color).IsValueType));
                }
            }

            // Ensure unique definitions and order them
            return definitions.Distinct().OrderBy(d => d.ClassName).ToList();
        }

        [MenuItem("Tools/VFX/Generate Property Classes")]
        public static void GeneratePropertyClasses()
        {
            string outputPath = EditorUtility.SaveFilePanel(
                "Save Generated VFX Properties",
                Application.dataPath,
                "VFXProperties.Generated.cs",
                "cs"
            );

            if (string.IsNullOrEmpty(outputPath))
                return;
            
            // Get property types dynamically using reflection
            List<PropertyTypeDefinition> propertyTypes = GetVFXPropertyDefinitions();

            StringBuilder sb = new StringBuilder();

            // File header
            sb.AppendLine("// This file is auto-generated by VFXPropertyGenerator.cs");
            sb.AppendLine("// DO NOT EDIT MANUALLY - Changes will be overwritten");
            sb.AppendLine("// To modify: Edit VFXPropertyGenerator.cs and regenerate");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.VFX;");
            sb.AppendLine();
            
            // Generate all the necessary classes
            GenerateAbstractBase(sb);

            foreach (var propType in propertyTypes)
            {
                GeneratePropertyClass(sb, propType);
            }

            // Write to file
            File.WriteAllText(outputPath, sb.ToString());
            AssetDatabase.Refresh();
            Debug.Log($"Generated VFX property classes at: {outputPath}");
        }

        static void GenerateAbstractBase(StringBuilder sb)
        {
            sb.AppendLine("[Serializable]");
            sb.AppendLine("public abstract class VFXProperty");
            sb.AppendLine("{");
            sb.AppendLine("    public string Name;");
            sb.AppendLine("    [NonSerialized] protected int m_nameID;");
            sb.AppendLine("    [NonSerialized] protected bool m_isInitialized;");
            sb.AppendLine();
            sb.AppendLine("    public abstract void ApplyToVFX(VisualEffect vfx);");
            sb.AppendLine();
            sb.AppendLine("    protected void Initialize()");
            sb.AppendLine("    {");
            sb.AppendLine("        if (string.IsNullOrEmpty(Name)) return;");
            sb.AppendLine("        m_nameID = Shader.PropertyToID(Name);");
            sb.AppendLine("        m_isInitialized = true;");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        static void GeneratePropertyClass(StringBuilder sb, PropertyTypeDefinition propType)
        {
            sb.AppendLine("[Serializable]");
            sb.AppendLine($"public class {propType.ClassName}VFXProperty : VFXProperty");
            sb.AppendLine("{");
            sb.AppendLine($"    public {propType.ValueType} Value;");
            
            // Use the IsValueType bool from the definition
            if (propType.IsValueType)
            {
                sb.AppendLine($"    [NonSerialized] {propType.ValueType} m_lastValue;");
            }

            sb.AppendLine();
            sb.AppendLine("    public override void ApplyToVFX(VisualEffect vfx)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (!m_isInitialized)");
            sb.AppendLine("        {");
            sb.AppendLine("            Initialize();");
            // For value types, set lastValue on first init to prevent first-frame skip
            if (propType.IsValueType)
            {
                sb.AppendLine("            m_lastValue = Value;");
            }
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        if (!m_isInitialized) return; // Guard against empty name");
            sb.AppendLine();

            // Optimization: Only set value types if they have changed.
            if (propType.IsValueType)
            {
                sb.AppendLine("        if (Value.Equals(m_lastValue))");
                sb.AppendLine("            return;");
                sb.AppendLine();
                sb.AppendLine("        m_lastValue = Value;");
            }
            
            // Use the correct Set method directly.
            sb.AppendLine($"        vfx.{propType.VFXSetMethod}(m_nameID, Value);");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
        }
    }
}
#endif