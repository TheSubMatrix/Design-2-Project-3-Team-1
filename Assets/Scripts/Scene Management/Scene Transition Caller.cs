using UnityEngine;

public class SceneTransitionCaller : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        SceneTransitionManager.Instance.TransitionToScene(sceneName);
    }
    public void ReloadScene()
    {
        SceneTransitionManager.Instance.ReloadScene();
    }
    public void ReloadScene(bool shouldTransition)
    {
        SceneTransitionManager.Instance.ReloadScene();
    }
}
