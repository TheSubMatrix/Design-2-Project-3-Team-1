using System.Collections;
using System.Collections.Generic;
using CustomNamespace.DependencyInjection;
using UnityEngine;

public class TutorialHandler : MonoBehaviour
{
    [Inject]
    IGoalEventProvider m_goal;
    [Inject]
    TutorialTextDisplay m_textDisplay;
    [Inject]
    IPlayerMovementEventProvider m_playerMovement;
    [Inject]
    IBowEventProvider m_bow;

    readonly List<TutorialStep> m_steps = new();

    public void Awake()
    {
        m_steps.Clear();

        m_steps.Add(TutorialStepDelegate<IPlayerMovementEventProvider.OnMove>.CreateAndInitialize(
            subscribe: (handler) => m_playerMovement.OnMoveEvent += handler,
            unsubscribe: (handler) => m_playerMovement.OnMoveEvent -= handler,
            onStepStarted: () => m_textDisplay.ShowTextCoroutine("Use WASD to move"),
            onStepEnded: () => m_textDisplay.HideTextCoroutine(),
            minimumTimeBeforeTransition: 2f
        ));
        m_steps.Add(TutorialStepDelegate<IPlayerMovementEventProvider.OnJump>.CreateAndInitialize(
            subscribe: (handler) => m_playerMovement.OnJumpEvent += handler,
            unsubscribe: (handler) => m_playerMovement.OnJumpEvent -= handler,
            onStepStarted: () => m_textDisplay.ShowTextCoroutine("Use the space bar to jump"),
            onStepEnded: () => m_textDisplay.HideTextCoroutine(),
            minimumTimeBeforeTransition: 2f
        ));
        m_steps.Add(TutorialStepDelegate<IBowEventProvider.OnBowCharge>.CreateAndInitialize(
            subscribe: (handler) => m_bow.OnBowChargeEvent += handler,
            unsubscribe: (handler) => m_bow.OnBowChargeEvent -= handler,
            onStepStarted: () => m_textDisplay.ShowTextCoroutine("Hold right click to charge up an arrow"),
            onStepEnded: () => m_textDisplay.HideTextCoroutine(),
            minimumTimeBeforeTransition: 2f
        ));
        m_steps.Add(TutorialStepTime.Create(
            duration: 4f,
            onStepStarted: () => m_textDisplay.ShowTextCoroutine("Use the mouse to aim the arrow"),
            onStepEnded: () => m_textDisplay.HideTextCoroutine()
        ));
        m_steps.Add(TutorialStepDelegate<IBowEventProvider.OnBowFire>.CreateAndInitialize(
            subscribe: (handler) => m_bow.OnBowFireEvent += handler,
            unsubscribe: (handler) => m_bow.OnBowFireEvent -= handler,
            onStepStarted: () => m_textDisplay.ShowTextCoroutine("While holding right click, press left click to fire"),
            onStepEnded: () => m_textDisplay.HideTextCoroutine(),
            minimumTimeBeforeTransition: 2f
        ));
        m_steps.Add(TutorialStepTime.Create(
            duration: 4f,
            onStepStarted: () => m_textDisplay.ShowTextCoroutine("Arrows will stick to wooden surfaces"),
            onStepEnded: () => m_textDisplay.HideTextCoroutine()
        ));
        m_steps.Add(TutorialStepDelegate<IBowEventProvider.OnBowArrowSelectionChanged>.CreateAndInitialize(
            subscribe:(handler) => m_bow.OnBowArrowSelectionChangedEvent += handler,
            unsubscribe: (handler) => m_bow.OnBowArrowSelectionChangedEvent -= handler,
            onStepStarted: () => m_textDisplay.ShowTextCoroutine("Use the scroll wheel to change arrows"),
            onStepEnded: () => m_textDisplay.HideTextCoroutine(),
            minimumTimeBeforeTransition: 2f
        ));
        m_steps.Add(TutorialStepTime.Create(
            duration: 4f,
            onStepStarted: () => m_textDisplay.ShowTextCoroutine("The bouncy arrow will bounce things on top of it upwards"),
            onStepEnded: () => m_textDisplay.HideTextCoroutine()
        ));
        m_steps.Add(TutorialStepTime.Create(
            duration: 4f,
            onStepStarted: () => m_textDisplay.ShowTextCoroutine("The rebound arrow will bounce off of non-wood surfaces"),
            onStepEnded: () => m_textDisplay.HideTextCoroutine()
        ));
        m_steps.Add(TutorialStepTime.Create(
            duration: 4f,
            onStepStarted: () => m_textDisplay.ShowTextCoroutine("The chain arrow will create a chain between two embedded arrows"),
            onStepEnded: () => m_textDisplay.HideTextCoroutine()
        ));
        m_steps.Add(TutorialStepUnityEvent<bool>.Create(
            unityEvent: m_goal.OnGoalStateChanged,
            onStepStarted: () => m_textDisplay.ShowTextCoroutine("Use the Arrows and your body to get the ball to the goal"),
            onStepEnded: () => m_textDisplay.HideTextCoroutine(),
            minimumTimeBeforeTransition: 2f
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
            yield return tutorialStep.ExecuteAsync();
        }
    }
}