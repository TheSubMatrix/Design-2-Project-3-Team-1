using System.Linq;
using CustomNamespace.DependencyInjection;
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
    string m_lastSelectedArrow = "";

    public void UpdateTrackedArrowName(string arrowName)
    {
        m_arrowName = arrowName;
    }
    void Awake()
    {
        m_currentRectTransform = GetComponent<RectTransform>();
        m_arrowCountStartingString = m_arrowCountText.text;
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
        if (quiverSelectionChangedEvent.Selected == m_arrowName && quiverSelectionChangedEvent.Selected != m_lastSelectedArrow)
        {
            m_currentRectTransform.localScale *= 1.1f;
        }
        if (quiverSelectionChangedEvent.Selected != m_arrowName)
        {
            m_currentRectTransform.localScale = Vector3.one;
        }
        m_lastSelectedArrow = quiverSelectionChangedEvent.Selected;
    }
    
}
