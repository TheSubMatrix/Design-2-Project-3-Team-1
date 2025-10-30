using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assembly = System.Reflection.Assembly;
#if UNITY_EDITOR
using UnityEditor.Compilation;
using UnityEngine;
#endif

public static class UserAssemblyUtil
{
    static void AddTypesFromAssembly(Type[] assemblyTypes, Type interfaceType, ICollection<Type> results)
    {
        if (assemblyTypes == null) return;
        foreach (Type type in assemblyTypes)
        {
            if (type == null) continue;
            if (type != interfaceType && interfaceType.IsAssignableFrom(type)) {
                results.Add(type);
            }
        }
    }

#if UNITY_EDITOR
    static HashSet<string> userAssemblyNames;

    static bool IsUserAssembly(Assembly asm)
    {
        if (userAssemblyNames == null)
        {
            userAssemblyNames = new HashSet<string>(StringComparer.Ordinal);
            string assetsPath = Application.dataPath.Replace('\\', '/') + "/";

            foreach (UnityEditor.Compilation.Assembly editorAsm in CompilationPipeline.GetAssemblies())
            {
                bool hasSourceInAssets = false;
                foreach (string src in editorAsm.sourceFiles)
                {
                    if (string.IsNullOrEmpty(src)) continue;
                    string p = src.Replace('\\', '/');
                    if (!p.StartsWith(assetsPath, StringComparison.OrdinalIgnoreCase)) continue;
                    hasSourceInAssets = true;
                    break;
                }

                if (hasSourceInAssets)
                {
                    userAssemblyNames.Add(editorAsm.name);
                }
            }
        }

        string name = asm.GetName().Name;
        return !string.IsNullOrEmpty(name) && userAssemblyNames.Contains(name);
    }
#else
    static bool IsUserAssembly(Assembly asm)
    {
        string name = asm.GetName().Name;
        if (string.IsNullOrEmpty(name)) return false;
        string[] excludePrefixes = {
            "Unity", "System", "mscorlib", "netstandard", "Mono.", "Microsoft.",
            "Boo.", "nunit", "ICSharpCode", "JetBrains", "Newtonsoft.", "YamlDotNet",
            "UnityEngine", "UnityEditor", "AK."
        };

        foreach (var p in excludePrefixes)
        {
            if (name.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        try
        {
            var _ = asm.Location;
        }
        catch
        {
            return true;
        }

        return true;
    }
#endif

    public static List<Type> GetTypes(Type interfaceType) 
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        HashSet<Type> unique = new HashSet<Type>();
        foreach (Assembly asm in assemblies)
        {
            if (!IsUserAssembly(asm))
                continue;

            Type[] asmTypes;
            try
            {
                asmTypes = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                asmTypes = ex.Types;
            }
            catch
            {
                continue;
            }

            AddTypesFromAssembly(asmTypes, interfaceType, unique);
        }

        return unique.ToList();
    }
}