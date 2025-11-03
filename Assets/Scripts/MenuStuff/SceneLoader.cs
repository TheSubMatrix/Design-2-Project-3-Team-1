using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [Header("References")]
    public ButtonClickArrow arrowAnimator;

    [Header("Settings")]
    public float delayAfterArrow = 0.5f;
    public float fadeDuration = 0.5f;

    public void OnButtonClicked(string sceneName)
    {
        var go = EventSystem.current.currentSelectedGameObject;
        if (!go)
        {
            Debug.LogWarning("No currentSelectedGameObject. Is there an EventSystem?");
            return;
        }

        RectTransform buttonRect = go.GetComponent<RectTransform>();
        if (!buttonRect)
        {
            Debug.LogWarning("Clicked object has no RectTransform.");
            return;
        }

        arrowAnimator.MoveArrowToButton(buttonRect, () =>
        {
            StartCoroutine(LoadSceneWithFade(sceneName));
        });
    }

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        yield return new WaitForSeconds(delayAfterArrow);

        if (ScreenFader.Instance)
            yield return ScreenFader.Instance.FadeOut(fadeDuration);

        SceneManager.LoadScene(sceneName);
        
    }
}