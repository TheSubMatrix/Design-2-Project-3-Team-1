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
        Func<IEnumerator> onStepEnded,
        float minimumTimeBeforeTransition = 0)
        : base(onStepStarted, onStepEnded, minimumTimeBeforeTransition)
    {
        m_subscribe = subscribe;
        m_unsubscribe = unsubscribe;
        m_handler = DelegateHelper.CreateHandler<TDelegate>(new Action(Complete));
    }

    protected override void OnStepEnded()
    {
        m_unsubscribe(m_handler);
    }
    
    public static TutorialStepDelegate<TDelegate> CreateAndInitialize(
        Action<TDelegate> subscribe,
        Action<TDelegate> unsubscribe,
        Func<IEnumerator> onStepStarted = null,
        Func<IEnumerator> onStepEnded = null,
        float minimumTimeBeforeTransition = 0
        )
    {
        TutorialStepDelegate<TDelegate> step = new TutorialStepDelegate<TDelegate>(subscribe, unsubscribe, onStepStarted, onStepEnded, minimumTimeBeforeTransition);
        Initialize(step);
        return step;
    }

    static void Initialize(TutorialStepDelegate<TDelegate> step)
    {
        step.m_subscribe(step.m_handler);
    }
}