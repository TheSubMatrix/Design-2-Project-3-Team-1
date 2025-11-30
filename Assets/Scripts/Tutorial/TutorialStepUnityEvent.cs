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
        Func<IEnumerator> onStepEnded)
        : base(onStepStarted, onStepEnded)
    {
        m_unityEvent = unityEvent;
        m_handler = Complete;
    }

    protected override void OnStepStarted()
    {
        m_unityEvent.AddListener(m_handler);
    }

    protected override void OnStepEnded()
    {
        m_unityEvent.RemoveListener(m_handler);
    }

    protected override IEnumerator WaitForCompletion()
    {
        yield return new WaitUntil(() => IsCompleted);
    }

    public static TutorialStepUnityEvent Create(
        UnityEvent unityEvent,
        Func<IEnumerator> onStepStarted = null,
        Func<IEnumerator> onStepEnded = null)
    {
        return new TutorialStepUnityEvent(unityEvent, onStepStarted, onStepEnded);
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
        Func<IEnumerator> onStepEnded)
        : base(onStepStarted, onStepEnded)
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

    protected override IEnumerator WaitForCompletion()
    {
        yield return new WaitUntil(() => IsCompleted);
    }

    public static TutorialStepUnityEvent<T> Create(
        UnityEvent<T> unityEvent,
        Action<T> onEventInvoked = null,
        Func<IEnumerator> onStepStarted = null,
        Func<IEnumerator> onStepEnded = null)
    {
        return new TutorialStepUnityEvent<T>(unityEvent, onEventInvoked, onStepStarted, onStepEnded);
    }
}