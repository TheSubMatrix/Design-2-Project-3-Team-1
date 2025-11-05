using AudioSystem;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class MainMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [FormerlySerializedAs("text")] public TMP_Text m_text;  
    [FormerlySerializedAs("normalColor")] public Color m_normalColor = Color.white;
    [FormerlySerializedAs("hoverColor")] public Color m_hoverColor = Color.yellow;
    [FormerlySerializedAs("hoverSound")] public SoundData m_hoverSound;

    void Start()
    {
        if (m_text == null)
            m_text = GetComponentInChildren<TMP_Text>();

        if (m_text != null)
            m_text.color = m_normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_text != null)
            m_text.color = m_hoverColor;
        SoundManager.Instance.CreateSound().WithSoundData(m_hoverSound).WithRandomPitch().Play();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (m_text != null)
            m_text.color = m_normalColor;
    }

    public void RequestTransitionToScene(string sceneName)
    {
        SceneTransitionManager.Instance.TransitionToScene(sceneName, 1f, SceneTransitionManager.TransitionType.Death);
    }
}