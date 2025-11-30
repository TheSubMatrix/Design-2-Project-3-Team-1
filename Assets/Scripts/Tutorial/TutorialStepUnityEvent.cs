using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TutorialStepUnityEvent : TutorialStep
{
    readonly UnityEvent m_unityEvent;
    readonly UnityAction m_handler;

    private TutorialStepUnityEvent(
        UnityEvent unityEvent,
        Func<IEnumerator> onStepStarted,
        Func<IEnumerator> onStepEnded,
        float minimumTimeBeforeTransition = 0)
        : base(onStepStarted, onStepEnded, minimumTimeBeforeTransition)
    {
        m_unityEvent = unityEvent;
        m_handler = Complete;
    }
    
    protected override void OnStepEnded()
    {
        m_unityEvent.RemoveListener(m_handler);
    }



    public static TutorialStepUnityEvent CreateAndInitialize(
        UnityEvent unityEvent,
        Func<IEnumerator> onStepStarted = null,
        Func<IEnumerator> onStepEnded = null,
        float minimumTimeBeforeTransition = 0
    )
    {
        TutorialStepUnityEvent step = new TutorialStepUnityEvent(unityEvent, onStepStarted, onStepEnded, minimumTimeBeforeTransition);
        Initialize(step);
        return step;
    }

    static void Initialize(TutorialStepUnityEvent tutorialStep)
    {
        tutorialStep.m_unityEvent.AddListener(tutorialStep.m_handler);
    }
}
public class TutorialStepUnityEvent<T> : TutorialStep
{
    readonly UnityEvent<T> m_unityEvent;
    readonly UnityAction<T> m_handler;

    private TutorialStepUnityEvent(
        UnityEvent<T> unityEvent,
        Action<T> onEventInvoked,
        Func<IEnumerator> onStepStarted,
        Func<IEnumerator> onStepEnded,
        float minimumTimeBeforeTransition = 0)
        : base(onStepStarted, onStepEnded, minimumTimeBeforeTransition)
    {
        m_unityEvent = unityEvent;
        Action<T> onEventInvoked1 = onEventInvoked;
        m_handler = (value) =>
        {
            onEventInvoked1?.Invoke(value);
            Complete();
        };
    }

    protected override void OnStepStarted()
    {
        m_unityEvent.AddListener(m_handler);
    }

    protected override void OnStepEnded()
    {
        m_unityEvent.RemoveListener(m_handler);
    }
    
    public static TutorialStepUnityEvent<T> Create(
        UnityEvent<T> unityEvent,
        Func<IEnumerator> onStepStarted = null,
        Func<IEnumerator> onStepEnded = null,
        Action<T> onEventInvoked = null,
        float minimumTimeBeforeTransition = 0
    )
        
    {
        TutorialStepUnityEvent<T> step = new TutorialStepUnityEvent<T>(unityEvent, onEventInvoked, onStepStarted, onStepEnded, minimumTimeBeforeTransition);
        Initialize(step);
        return step;
    }
    static void Initialize(TutorialStepUnityEvent<T> tutorialStep)
    {
        tutorialStep.m_unityEvent.AddListener(tutorialStep.m_handler);
    }
}