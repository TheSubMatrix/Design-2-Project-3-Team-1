using System.Collections;
using System.Collections.Generic;
using CustomNamespace.DependencyInjection;
using UnityEngine;

public class TutorialHandler : MonoBehaviour
{
    [Inject]
    TutorialTextDisplay m_textDisplay;
    [Inject]
    IPlayerMovementEventProvider m_playerMovement;
    [Inject]
    IBowEventProvider m_bow;
    List<TutorialStep> m_steps = new();

    public void Awake()
    {
        m_steps.Clear();

        m_steps.Add(TutorialStep.Create<IPlayerMovementEventProvider.OnMove>(
            subscribe: (handler) => m_playerMovement.OnMoveEvent += handler,
            unsubscribe: (handler) => m_playerMovement.OnMoveEvent -= handler,
            onStepStarted: () => m_textDisplay.ShowTextCoroutine("Use WASD to move"),
            onStepEnded: () => m_textDisplay.HideTextCoroutine()
        ));
        m_steps.Add(TutorialStep.Create<IBowEventProvider.OnBowCharge>(
            subscribe: (handler) => m_bow.OnBowChargeEvent += handler,
            unsubscribe: (handler) => m_bow.OnBowChargeEvent -= handler,
            onStepStarted: () => m_textDisplay.ShowTextCoroutine("Hold right click to charge"),
            onStepEnded: () => m_textDisplay.HideTextCoroutine()
        ));
        m_steps.Add(TutorialStep.Create<IBowEventProvider.OnBowFire>(
            subscribe: (handler) => m_bow.OnBowFireEvent += handler,
            unsubscribe: (handler) => m_bow.OnBowFireEvent -= handler,
            onStepStarted: () => m_textDisplay.ShowTextCoroutine("While holding right click, press left click to fire"),
            onStepEnded: () => m_textDisplay.HideTextCoroutine()
        ));
    }
    
    void Start()
    {
        StartCoroutine(TutorialCoroutine());
    }
    
    IEnumerator TutorialCoroutine()
    {
        foreach (TutorialStep tutorialStep in m_steps)
        {
            yield return tutorialStep.TutorialStepSequenceAsync();
        }
    }
}