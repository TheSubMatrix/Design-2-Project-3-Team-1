using CustomNamespace.LowLevel;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace CustomNamespace.Timers
{
    internal static class TimerBootstrapper
    {
        static PlayerLoopSystem s_timerSystem;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void Initialize()
        {
            PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
            if (!InsertTimerManager<Update>(ref currentPlayerLoop, 0))
            {
                Debug.LogWarning("Timer System could not be initialized, Unable to register TimerManager into Update loop");
                return;
            }
            PlayerLoop.SetPlayerLoop(currentPlayerLoop);
            
            #if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            static void OnPlayModeStateChanged(PlayModeStateChange state)
            {
                if (state != PlayModeStateChange.ExitingPlayMode) return;
                PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
                RemoveTimerManager<Update>(ref currentPlayerLoop);
                TimerManager.Clear();
            }
            #endif
        }

        static void RemoveTimerManager<T>(ref PlayerLoopSystem currentPlayerLoop)
        {
            PlayerLoopUtils.RemoveSystem<T>(ref currentPlayerLoop, in s_timerSystem);
        }
        static bool InsertTimerManager<T>(ref PlayerLoopSystem loop, int index)
        {
            s_timerSystem = new PlayerLoopSystem
            {
                type = typeof(TimerManager),
                updateDelegate = TimerManager.UpdateTimers,
                subSystemList = null
            };
            return PlayerLoopUtils.InsertSystem<T>(ref loop, in s_timerSystem, index);
        }
        
    }
}
