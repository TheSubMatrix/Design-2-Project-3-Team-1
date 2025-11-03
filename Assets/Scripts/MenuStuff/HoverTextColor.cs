using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class ButtonHoverEffectTMP : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TMP_Text text;  
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public AudioSource hoverSound;

    void Start()
    {
        if (text == null)
            text = GetComponentInChildren<TMP_Text>();

        if (text != null)
            text.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (text != null)
            text.color = hoverColor;

        if (hoverSound != null && !hoverSound.isPlaying)
            hoverSound.Play();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (text != null)
            text.color = normalColor;
    }
}