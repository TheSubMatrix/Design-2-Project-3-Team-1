using System;
using CustomNamespace.DependencyInjection;
using UnityEngine;
[RequireComponent(typeof(Collider2D))]
public class LevelCompleteTrigger : MonoBehaviour
{
    [Inject] ILevelDataProvider m_levelDataProvider;
    [SerializeField]string m_nextLevel;
    Collider2D m_nextLevelTrigger;
    void Start()
    {
        m_nextLevel = m_levelDataProvider.GetNextLevel();
        m_nextLevelTrigger = GetComponent<Collider2D>();
        m_nextLevelTrigger.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.layer != LayerMask.NameToLayer("Player")){return;}
        SceneTransitionManager.Instance.TransitionToScene(m_nextLevel);
    }
}
