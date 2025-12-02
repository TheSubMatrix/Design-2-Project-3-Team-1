using System.Collections;
using UnityEngine;
[RequireComponent(typeof(CanvasGroup))]
public class CreditsBox : MonoBehaviour
{
    [SerializeField] float m_fadeTime = 0.5f;
    CanvasGroup m_controlledCanvasGroup;
    Coroutine m_fadeRoutine;
    void Start()
    {
        m_controlledCanvasGroup = GetComponent<CanvasGroup>();
    }
    public void UpdateDesiredVisibility(bool newVisibility)
    {
        if (m_fadeRoutine != null)
        {
            StopCoroutine(m_fadeRoutine);
        }
        StartCoroutine(FadeCanvasGroupAsync(newVisibility));
    }
    
    IEnumerator FadeCanvasGroupAsync(bool newVisibility)
    {
        float elapsedTime = 0;
        float startingAlpha = m_controlledCanvasGroup.alpha;
        while (elapsedTime <= m_fadeTime)
        {
            m_controlledCanvasGroup.alpha = Mathf.Lerp(startingAlpha, newVisibility ? 1f : 0f, elapsedTime / m_fadeTime);
            yield return null;
            elapsedTime += Time.deltaTime;
        }
    }
}
