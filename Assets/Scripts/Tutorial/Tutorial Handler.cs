using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomNamespace.DependencyInjection;
using UnityEngine;

public class TutorialHandler : MonoBehaviour
{
    [Inject]
    IPlayerMovementProvider m_playerMovement;
    [Serializable]
    public class TutorialStep
    {
        bool m_isCompleted;
        public Action StepCompletedCallback => StepCallback;
        void StepCallback()
        {
            m_isCompleted = true;
        }
        public IEnumerator WaitUntilStepCompleted()
        {
            m_isCompleted = false;
            yield return new WaitUntil(() => m_isCompleted);
        }
    }
    List<TutorialStep> m_steps = new();
    
    IEnumerator TutorialCoroutine()
    {
        return m_steps.Select(step => step.WaitUntilStepCompleted()).GetEnumerator();
    }
}
