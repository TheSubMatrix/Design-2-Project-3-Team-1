using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class SceneLoader : MonoBehaviour
{
    public ButtonClickArrow arrowAnimator; // Drag UIController (with ButtonClickArrow) here

    // Call this from each Button’s OnClick, passing the scene name string
    public void OnButtonClicked(string sceneName)
    {
        // Which button was clicked?
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

        // Animate arrow, then load scene
        arrowAnimator.MoveArrowToButton(buttonRect, () =>
        {
            SceneManager.LoadScene(sceneName);
        });
    }
}