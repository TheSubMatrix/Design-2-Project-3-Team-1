using System;
using System.Collections;
using UnityEngine.Events;

public static class UnityEventCoroutineExtensions
{
    public static IEnumerator ToCoroutine(this UnityEvent unityEvent, Action onComplete = null)
    {
        return DelegateExtensions.ToCoroutine<UnityAction>(
            unityEvent.AddListener,
            unityEvent.RemoveListener,
            onComplete
        );
    }
    
    public static IEnumerator ToCoroutine<T>(this UnityEvent<T> unityEvent, Action<T> onComplete = null)
    {
        return DelegateExtensions.ToCoroutine<UnityAction<T>>(
            unityEvent.AddListener,
            unityEvent.RemoveListener,
            onComplete
        );
    }
    
    public static IEnumerator ToCoroutine<T0, T1>(this UnityEvent<T0, T1> unityEvent, Action<T0, T1> onComplete = null)
    {
        return DelegateExtensions.ToCoroutine<UnityAction<T0, T1>>(
            unityEvent.AddListener,
            unityEvent.RemoveListener,
            onComplete
        );
    }
    
    public static IEnumerator ToCoroutine<T0, T1, T2>(this UnityEvent<T0, T1, T2> unityEvent, Action<T0, T1, T2> onComplete = null)
    {
        return DelegateExtensions.ToCoroutine<UnityAction<T0, T1, T2>>(
            unityEvent.AddListener,
            unityEvent.RemoveListener,
            onComplete
        );
    }
    
    public static IEnumerator ToCoroutine<T0, T1, T2, T3>(this UnityEvent<T0, T1, T2, T3> unityEvent, Action<T0, T1, T2, T3> onComplete = null)
    {
        return DelegateExtensions.ToCoroutine<UnityAction<T0, T1, T2, T3>>(
            unityEvent.AddListener,
            unityEvent.RemoveListener,
            onComplete
        );
    }
}