using CustomNamespace.DependencyInjection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour, IDependencyProvider, ILevelDataProvider
{
    [SerializeField] LevelConfigSO m_levelConfig;
    
    LevelData m_currentLevelData;
    
    void Awake()
    {
        if(m_currentLevelData is null){ LoadCurrentLevelData();}
        SceneManager.activeSceneChanged += OnSceneChanged;
    }
    
    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }
    
    void OnSceneChanged(Scene current, Scene next)
    {
        LoadLevelData(next.name);
    }

    void LoadCurrentLevelData()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        LoadLevelData(sceneName);   
    }
    void LoadLevelData(string sceneName)
    {
        m_currentLevelData = m_levelConfig.GetLevelData(sceneName);

        if (m_currentLevelData == null)
        {
            Debug.LogWarning($"No level data found for scene: {sceneName}");
        }
    }
    
    [Provide]
    public ILevelDataProvider ProvideLevelDataProvider()
    {
        if (m_currentLevelData != null) return this;
        LoadCurrentLevelData();
        return this;
    } 
    
    public uint GetArrowCounts(Arrow arrow)
    {
        return m_currentLevelData?.GetArrowCounts(arrow) ?? 0;
    }

    public string GetNextLevel()
    {
        return m_currentLevelData.GetNextLevel();
    }
}
