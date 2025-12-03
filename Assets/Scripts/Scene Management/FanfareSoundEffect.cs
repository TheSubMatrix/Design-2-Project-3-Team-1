using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FanfareSoundEffect : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    void OnSceneChanged(Scene current, Scene next)
    {
        if (next.name != "Win Scene" && next.name != "NewCreditsScene")
        {
            Destroy(gameObject);
        }
    }
}
