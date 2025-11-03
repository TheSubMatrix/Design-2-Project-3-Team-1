using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;
    public CanvasGroup fadeOverlay;
    public float defaultFadeDuration = 0.5f;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Fade in automatically when a new scene loads
        if (fadeOverlay != null)
            StartCoroutine(FadeIn(defaultFadeDuration));
    }

    public IEnumerator FadeOut(float duration = -1f)
    {
        if (duration <= 0f) duration = defaultFadeDuration;
        if (!fadeOverlay) yield break;

        fadeOverlay.blocksRaycasts = true;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            fadeOverlay.alpha = Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }

        fadeOverlay.alpha = 1f;
    }

    public IEnumerator FadeIn(float duration = -1f)
    {
        if (duration <= 0f) duration = defaultFadeDuration;
        if (!fadeOverlay) yield break;

        fadeOverlay.blocksRaycasts = true;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            fadeOverlay.alpha = Mathf.SmoothStep(1f, 0f, t);
            yield return null;
        }

        fadeOverlay.alpha = 0f;
        fadeOverlay.blocksRaycasts = false;
    }
}