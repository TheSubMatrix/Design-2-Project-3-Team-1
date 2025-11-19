using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArrowDisplay : MonoBehaviour
{
    [SerializeField] string m_arrowName;
    [SerializeField] Image m_arrowImage;
    [SerializeField] TMP_Text m_arrowCountText;
    RectTransform m_currentRectTransform;
    EventBinding<QuiverUpdatedEvent> m_quiverUpdatedBinding;
    EventBinding<QuiverSelectionChangedEvent> m_selectionChangedBinding;
    string m_arrowCountStartingString = "";
    void Awake()
    {
        m_arrowCountStartingString = m_arrowCountText.text;
        m_currentRectTransform = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        m_quiverUpdatedBinding = new EventBinding<QuiverUpdatedEvent>(UpdateArrowDisplay);
        m_selectionChangedBinding = new EventBinding<QuiverSelectionChangedEvent>(UpdateSelectionDisplay);
        EventBus<QuiverUpdatedEvent>.Register(m_quiverUpdatedBinding);
        EventBus<QuiverSelectionChangedEvent>.Register(m_selectionChangedBinding);
    }
    void OnDisable()
    {
        EventBus<QuiverUpdatedEvent>.Deregister(m_quiverUpdatedBinding);
        EventBus<QuiverSelectionChangedEvent>.Deregister(m_selectionChangedBinding);
    }
    void UpdateArrowDisplay(QuiverUpdatedEvent quiverUpdatedEvent)
    {
        if(quiverUpdatedEvent.Name != m_arrowName) return;
        m_arrowImage.sprite = quiverUpdatedEvent.Sprite;
        m_arrowCountText.text = m_arrowCountStartingString + quiverUpdatedEvent.Arrows;
    }

    void UpdateSelectionDisplay(QuiverSelectionChangedEvent quiverSelectionChangedEvent)
    {
        if (quiverSelectionChangedEvent.Selected == m_arrowName)
        {
            m_currentRectTransform.localScale *= 1.1f;
        }
        else
        {
            m_currentRectTransform.localScale = Vector3.one;
        }
    }
    
}
