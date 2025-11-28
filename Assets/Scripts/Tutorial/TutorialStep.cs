using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;


public class TutorialStep
{
    public bool IsRunning { get; private set; }
    
    bool m_isCompleted;

    Action m_enableListening;
    Action m_disableListening;

    readonly Func<IEnumerator> m_onStepStarted;
    readonly Func<IEnumerator> m_onStepEnded;
        
    private TutorialStep(Func<IEnumerator> onStepStarted, Func<IEnumerator> onStepEnded)
    {
        m_onStepStarted = onStepStarted;
        m_onStepEnded = onStepEnded;
    }
    
    ~TutorialStep()
    {
        m_disableListening?.Invoke();
    }

    private void Complete()
    {
        if(IsRunning)
        {
            m_isCompleted = true;
        }
    }
    
    public void ForceComplete()
    {
        if (IsRunning && !m_isCompleted)
        {
            Complete();
        }
    }

    public IEnumerator TutorialStepSequenceAsync()
    {
        m_isCompleted = false;
        IsRunning = true;
        if (m_onStepStarted != null)
            yield return m_onStepStarted.Invoke();
        m_enableListening?.Invoke();
        yield return new WaitUntil(() => m_isCompleted);
        m_disableListening?.Invoke();
        if (m_onStepEnded != null)
            yield return m_onStepEnded.Invoke();
        
        IsRunning = false;
    }
    static TDelegate CreateHandler<TDelegate>(Action onComplete) where TDelegate : Delegate
    {
        MethodInfo invoke = typeof(TDelegate).GetMethod("Invoke");
        ParameterInfo[] parameters = invoke?.GetParameters();
        Expression<TDelegate> lambda = Expression.Lambda<TDelegate>(
            Expression.Call(Expression.Constant(onComplete), nameof(Action.Invoke), null),
            (parameters ?? Array.Empty<ParameterInfo>()).Select(p => Expression.Parameter(p.ParameterType, p.Name))
        );

        return lambda.Compile();
    }
    public static TutorialStep Create<T>(
        Action<T> subscribe,
        Action<T> unsubscribe,
        Func<IEnumerator> onStepStarted,
        Func<IEnumerator> onStepEnded)
        where T : Delegate
    {
        TutorialStep step = new TutorialStep(onStepStarted, onStepEnded);

        T handler = CreateHandler<T>(step.Complete);

        step.m_enableListening = () => subscribe(handler);
        step.m_disableListening = () => unsubscribe(handler);

        return step;
    }
}