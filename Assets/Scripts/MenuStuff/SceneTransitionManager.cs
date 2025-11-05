using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : PersistentSingleton<SceneTransitionManager>
{
    static readonly int s_TransitionTime = Shader.PropertyToID("_Transition_Time");
    [SerializeField] Material m_deathTransitionMaterial;
    
    bool m_isTransitioning;
    
    public enum TransitionType
    {
        Fade,
        Death
    }
    public void TransitionToScene(string sceneName, float transitionDuration, TransitionType transitionType)
    {
        if(m_isTransitioning) return;
        if (transitionType == TransitionType.Death)
        {
            StartCoroutine(DeathFadeTransitionSequence(sceneName, transitionDuration));
        }
        else
        {
            
        }
    }

    IEnumerator DeathFadeTransitionSequence(string sceneName, float fadeDuration)
    {
        m_isTransitioning = true;
        yield return FadeDeath( fadeDuration / 2, 0.5f);
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        yield return FadeDeath( fadeDuration / 2, 1f);
        m_deathTransitionMaterial.SetFloat(s_TransitionTime, 0);
        m_isTransitioning = false;
    }
    IEnumerator FadeDeath(float fadeDuration, float destinationFadePoint)
    {
        
        float currentFadePercent = m_deathTransitionMaterial.GetFloat(s_TransitionTime);
        float currentFadeTime = 0f;
        while (currentFadeTime < fadeDuration)
        {
            currentFadeTime += Time.deltaTime;
            m_deathTransitionMaterial.SetFloat(s_TransitionTime, Mathf.SmoothStep(currentFadePercent, destinationFadePoint, currentFadeTime / fadeDuration));
            yield return null;
        }
    }

    void OnDestroy()
    {
        m_deathTransitionMaterial.SetFloat(s_TransitionTime, 0);
    }

    void OnDisable()
    {
        m_deathTransitionMaterial.SetFloat(s_TransitionTime, 0);
    }
}
