using System.Collections.Generic;
using System.Linq;
using CustomNamespace.DependencyInjection;
using UnityEngine;

public class ArrowDisplayManager : MonoBehaviour
{
    [SerializeField] ArrowDisplay m_displayPrefab;
    readonly Dictionary<string, ArrowDisplay> m_displaysByName = new();
    
    [Inject]
    // ReSharper disable once UnusedMember.Local
    //This is used by the Dependency Injection Framework
    void OnReceivedLevelData(ILevelDataProvider levelData)
    {
        foreach (KeyValuePair<Arrow, uint> kvp in levelData.GetArrowCounts())
        {
            if (m_displaysByName.ContainsKey(kvp.Key.name)) continue;
            ArrowDisplay display = Instantiate(m_displayPrefab, transform).GetComponent<ArrowDisplay>();
            display.UpdateTrackedArrowName(kvp.Key);
            m_displaysByName.Add(kvp.Key.name, display);
        }
    }
}
