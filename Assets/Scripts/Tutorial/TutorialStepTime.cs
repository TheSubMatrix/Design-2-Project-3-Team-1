using System;
using System.Collections;
using UnityEngine;

public class TutorialStepTime : TutorialStep
{
    
    private TutorialStepTime(
        float duration,
        Func<IEnumerator> onStepStarted,
        Func<IEnumerator> onStepEnded)
        : base(onStepStarted, onStepEnded, duration)
    {
        
    }

    protected override bool CheckCompletionState()
    {
        return true;
    }
    public static TutorialStepTime Create(
        float duration,
        Func<IEnumerator> onStepStarted = null,
        Func<IEnumerator> onStepEnded = null)
    {
        return new TutorialStepTime(duration, onStepStarted, onStepEnded);
    }
}