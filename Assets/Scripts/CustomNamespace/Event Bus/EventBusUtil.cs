using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class EventBusUtil
{
    public static IReadOnlyList<Type> EventTypes { get; set; }
    public static IReadOnlyList<Type> EventBusTypes { get; set; }
    
    #if UNITY_EDITOR
        public static PlayModeStateChange PlayModeState { get; set; }
        [InitializeOnLoadMethod]
        public static void InitializeEditor()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged; 
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            PlayModeState = state;
            if (state == PlayModeStateChange.ExitingPlayMode)
                ClearAllBusses();
        }
    #endif
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        EventTypes = UserAssemblyUtil.GetTypes(typeof(IEvent));
        EventBusTypes = InitializeAllBusses();
    }

    static List<Type> InitializeAllBusses()
    {
        List<Type> eventBusTypes = new();
        Type typedef = typeof(EventBus<>);
        foreach (Type eventType in EventTypes)
        {
            Type busType = typedef.MakeGenericType(eventType);
            eventBusTypes.Add(busType);
        }

        return eventBusTypes;
    }

    public static void ClearAllBusses()
    {
        Debug.Log("Clearing all event busses");
        foreach (Type busType in EventBusTypes)
        {
            MethodInfo clearMethod = busType.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            clearMethod?.Invoke(null, null);
        }
    }
}