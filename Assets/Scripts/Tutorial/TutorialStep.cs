using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public abstract class TutorialStep
{
    public bool IsRunning { get; private set; }
    protected bool IsCompleted;
    protected readonly float MinimumTimeBeforeCompleted = 0;
    readonly Func<IEnumerator> m_onStepStarted;
    readonly Func<IEnumerator> m_onStepEnded;

    protected TutorialStep(Func<IEnumerator> onStepStarted, Func<IEnumerator> onStepEnded, float minimumTimeBeforeCompleted = 0)
    {
        m_onStepStarted = onStepStarted;
        m_onStepEnded = onStepEnded;
        MinimumTimeBeforeCompleted = minimumTimeBeforeCompleted;
    }

    protected virtual void Complete()
    {
        IsCompleted = true;
    }
    
    public void ForceComplete()
    {
        if (IsRunning && !IsCompleted)
        {
            Complete();
        }
    }

    public IEnumerator ExecuteAsync()
    {
        IsRunning = true;
        
        if (m_onStepStarted != null)
            yield return m_onStepStarted.Invoke();
        
        OnStepStarted();
        float currentTime = 0;
        while (!CheckCompletionState() || currentTime < MinimumTimeBeforeCompleted)
        {
            yield return null;
            currentTime += Time.deltaTime;
        }
        OnStepEnded();
        
        if (m_onStepEnded != null)
            yield return m_onStepEnded.Invoke();
        
        IsRunning = false;
    }

    /// <summary>
    /// Called when the step starts, before waiting for completion
    /// </summary>
    protected virtual void OnStepStarted() { }

    /// <summary>
    /// Called when the step ends, after completion
    /// </summary>
    protected virtual void OnStepEnded() { }
    
    protected virtual bool CheckCompletionState()
    {
        return IsCompleted;
    }
}
