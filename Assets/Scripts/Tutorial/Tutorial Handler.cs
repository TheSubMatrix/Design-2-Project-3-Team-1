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
            converter: (completeAction) => () => completeAction(),
            onStepStarted: () => m_textDisplay.ShowTextCoroutine("Use WASD to move"),
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