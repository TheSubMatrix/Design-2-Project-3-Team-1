using System;
using System.Collections;
using UnityEngine;

public class DelegateExtensions : MonoBehaviour
{
    internal static IEnumerator ToCoroutine<TDelegate>(
        Action<TDelegate> subscribe,
        Action<TDelegate> unsubscribe,
        Delegate onComplete = null) where TDelegate : Delegate
    {
        bool eventFired = false;

        TDelegate handler = onComplete != null ?
            DelegateHelper.CreateHandlerWithCallback<TDelegate>(onComplete, () => eventFired = true) :
            DelegateHelper.CreateHandler<TDelegate>(new Action(() => eventFired = true));
        
        try
        {
            subscribe(handler);
            yield return new WaitUntil(() => eventFired);
        }
        finally
        {
            unsubscribe(handler);
        }
    }
}