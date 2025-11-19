using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelData
{
    [SerializeField] SerializableDictionary<Arrow, uint> m_arrowCounts;
    [SerializeField] string m_nextLevel = "";
    public uint GetArrowCounts(Arrow arrow)
    {
        return m_arrowCounts.TryGetValue(arrow, out uint count) ? count : (uint)0;
    }
    public string GetNextLevel()
    {
        return m_nextLevel;
    }

    public Dictionary<Arrow, uint> GetArrowCounts()
    {
        return m_arrowCounts;
    }
}