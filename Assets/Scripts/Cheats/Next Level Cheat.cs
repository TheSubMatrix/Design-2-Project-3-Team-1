using CustomNamespace.DependencyInjection;
using UnityEngine;
using UnityEngine.InputSystem;

public class NextLevelCheat : PersistentSingleton<NextLevelCheat>
{
    [SerializeField, RequiredField] InputActionReference m_nextLevelInputAction;
    [Inject] ILevelDataProvider m_levelDataProvider;

    void OnEnable()
    {
        m_nextLevelInputAction.action.Enable();
        m_nextLevelInputAction.action.started += OnNextLevelInput;
    }

    void OnDisable()
    {
        m_nextLevelInputAction.action.started -= OnNextLevelInput;
        m_nextLevelInputAction.action.Disable();
    }

    void OnNextLevelInput(InputAction.CallbackContext context)
    {
        string nextLevel = m_levelDataProvider.GetNextLevel();
        if(string.IsNullOrEmpty(nextLevel)){return;}
        SceneTransitionManager.Instance.TransitionToScene(m_levelDataProvider.GetNextLevel());
    }
}
