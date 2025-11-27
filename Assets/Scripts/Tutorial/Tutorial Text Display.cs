using System.Collections;
using CustomNamespace.DependencyInjection;
using UnityEngine;
using TMPro;

public class TutorialTextDisplay : MonoBehaviour, IDependencyProvider
{
    [Provide]
    TutorialTextDisplay ProvideSelf()
    {
        return this;
    }
    [SerializeField, RequiredField] TextMeshProUGUI m_textMesh;
    [SerializeField, RequiredField] CanvasGroup m_canvasGroup;
    [SerializeField] float m_fadeDuration = 0.5f;

    Coroutine m_activeFadeCoroutine;

    void Awake()
    {
        // Ensure invisible at start
        if(m_canvasGroup != null) m_canvasGroup.alpha = 0; 
    }

    public IEnumerator ShowTextCoroutine(string text)
    {
        m_textMesh.text = text;
        return FadeTo(1.0f);
    }

    public IEnumerator HideTextCoroutine()
    {
        return FadeTo(0.0f);
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (m_activeFadeCoroutine != null)
        {
            StopCoroutine(m_activeFadeCoroutine);
        }
        float startAlpha = m_canvasGroup.alpha;
        float time = 0f;

        while (time < m_fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / m_fadeDuration;
            m_canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        m_canvasGroup.alpha = targetAlpha;
        m_activeFadeCoroutine = null;
    }
}