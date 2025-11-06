using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Level Config", fileName = "New Level Config")]
public class LevelConfigSO : ScriptableObject
{
    [SerializeField] SerializableDictionary<string, LevelData> m_sceneData;
    
    public LevelData GetLevelData(string sceneName)
    {
        return m_sceneData.TryGetValue(sceneName, out LevelData data) ? data : null;
    }
}