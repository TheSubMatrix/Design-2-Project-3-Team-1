using System;
using System.Collections;
using UnityEngine.Events;

public abstract class TutorialStep
{
    public bool IsRunning { get; private set; }
    
    protected bool IsCompleted;

    readonly Func<IEnumerator> m_onStepStarted;
    readonly Func<IEnumerator> m_onStepEnded;
        
    protected TutorialStep(Func<IEnumerator> onStepStarted, Func<IEnumerator> onStepEnded)
    {
        m_onStepStarted = onStepStarted;
        m_onStepEnded = onStepEnded;
    }

    protected virtual void Complete()
    {
        if(IsRunning)
        {
            IsCompleted = true;
        }
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
        IsCompleted = false;
        IsRunning = true;
        
        if (m_onStepStarted != null)
            yield return m_onStepStarted.Invoke();
        
        OnStepStarted();
        
        yield return WaitForCompletion();
        
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

    /// <summary>
    /// Returns the coroutine that waits for this step to complete
    /// </summary>
    protected abstract IEnumerator WaitForCompletion();
}
