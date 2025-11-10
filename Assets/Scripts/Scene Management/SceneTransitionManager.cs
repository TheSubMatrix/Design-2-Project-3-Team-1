using System.Collections;
using AudioSystem;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : PersistentSingleton<SceneTransitionManager>
{
    static readonly int s_TransitionTime = Shader.PropertyToID("_Transition_Time");
    [SerializeField] AudioMixerGroup m_musicMixerGroup;
    [SerializeField] AudioMixerGroup m_sfxMixerGroup;
    [SerializeField] SoundData m_transitionSound;
    [SerializeField] Material m_deathTransitionMaterial;
    
    bool m_isTransitioning;
    
    public void TransitionToScene(string sceneName, float transitionDuration = 1)
    {
        if(m_isTransitioning) return;
        StartCoroutine(DeathFadeTransitionSequence(sceneName, transitionDuration));

    }
    public void ReloadScene(float transitionDuration = 1)
    {
        if(m_isTransitioning) return;
        StartCoroutine(DeathFadeTransitionSequence(SceneManager.GetActiveScene().name, transitionDuration));
    }
    
    IEnumerator DeathFadeTransitionSequence(string sceneName, float fadeDuration)
    {
        m_isTransitioning = true;
        SoundManager.Instance.CreateSound().WithSoundData(m_transitionSound).WithRandomPitch().Play();
        m_sfxMixerGroup.audioMixer.GetFloat("Sound Effect Volume", out float sfxVolume);
        m_musicMixerGroup.audioMixer.GetFloat("Music Volume", out float musicVolume);
        yield return FadeDeathAndSounds( fadeDuration / 2, 0.5f, 0, 0);
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        yield return FadeDeathAndSounds( fadeDuration / 2, 1f, sfxVolume, musicVolume);
        m_deathTransitionMaterial.SetFloat(s_TransitionTime, 0);
        m_isTransitioning = false;
    }
    IEnumerator FadeDeathAndSounds(float fadeDuration, float destinationFadePoint, float sfxDestinationVolume, float musicDestinationVolume)
    {
        
        m_sfxMixerGroup.audioMixer.GetFloat("Sound Effect Volume", out float sfxVolume);
        m_musicMixerGroup.audioMixer.GetFloat("Music Volume", out float musicVolume);
        float currentFadePercent = m_deathTransitionMaterial.GetFloat(s_TransitionTime);
        float currentFadeTime = 0f;
        while (currentFadeTime < fadeDuration)
        {
            currentFadeTime += Time.deltaTime;
            m_sfxMixerGroup.audioMixer.SetFloat("Sound Effect Volume", Mathf.Lerp(sfxVolume, sfxDestinationVolume, currentFadePercent));
            m_musicMixerGroup.audioMixer.SetFloat("Music Volume", Mathf.Lerp(musicVolume, musicDestinationVolume, currentFadePercent));
            m_deathTransitionMaterial.SetFloat(s_TransitionTime, Mathf.SmoothStep(currentFadePercent, destinationFadePoint, currentFadeTime / fadeDuration));
            yield return null;
        }
    }

    void OnDestroy()
    {
        if(CurrentInstance != this){return;}
        m_deathTransitionMaterial.SetFloat(s_TransitionTime, 0);
    }

    void OnDisable()
    {
        if(CurrentInstance != this){return;}
        m_deathTransitionMaterial.SetFloat(s_TransitionTime, 0);
    }
}
