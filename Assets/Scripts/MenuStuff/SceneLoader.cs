using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class SceneLoader : MonoBehaviour
{
    public ButtonClickArrow arrowAnimator; 

   
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
            SceneManager.LoadScene(sceneName);
        });
    }
}