using System;
using System.Collections;
using UnityEngine;

public class TutorialStepTime : TutorialStep
{
    readonly float m_duration;

    private TutorialStepTime(
        float duration,
        Func<IEnumerator> onStepStarted,
        Func<IEnumerator> onStepEnded)
        : base(onStepStarted, onStepEnded)
    {
        m_duration = duration;
    }

    protected override IEnumerator WaitForCompletion()
    {
        float elapsed = 0f;
        while (elapsed < m_duration && !IsCompleted)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        IsCompleted = true;
    }

    public static TutorialStepTime Create(
        float duration,
        Func<IEnumerator> onStepStarted = null,
        Func<IEnumerator> onStepEnded = null)
    {
        return new TutorialStepTime(duration, onStepStarted, onStepEnded);
    }
}