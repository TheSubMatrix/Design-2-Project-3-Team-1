using System;
using System.Collections;
using UnityEngine;

public class TutorialStepDelegate<TDelegate> : TutorialStep where TDelegate : Delegate
{
    readonly Action<TDelegate> m_subscribe;
    readonly Action<TDelegate> m_unsubscribe;
    readonly TDelegate m_handler;

    TutorialStepDelegate(
        Action<TDelegate> subscribe,
        Action<TDelegate> unsubscribe,
        Func<IEnumerator> onStepStarted,
        Func<IEnumerator> onStepEnded)
        : base(onStepStarted, onStepEnded)
    {
        m_subscribe = subscribe;
        m_unsubscribe = unsubscribe;
        m_handler = DelegateHelper.CreateHandler<TDelegate>(new Action(Complete));
    }

    protected override void OnStepStarted()
    {
        m_subscribe(m_handler);
    }

    protected override void OnStepEnded()
    {
        m_unsubscribe(m_handler);
    }

    protected override IEnumerator WaitForCompletion()
    {
        yield return new WaitUntil(() => IsCompleted);
    }

    public static TutorialStepDelegate<TDelegate> Create(
        Action<TDelegate> subscribe,
        Action<TDelegate> unsubscribe,
        Func<IEnumerator> onStepStarted = null,
        Func<IEnumerator> onStepEnded = null)
    {
        return new TutorialStepDelegate<TDelegate>(subscribe, unsubscribe, onStepStarted, onStepEnded);
    }
}