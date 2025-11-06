using System;
using UnityEngine;

[Serializable]
public class LevelData
{
    [SerializeField] SerializableDictionary<Arrow, uint> m_arrowCounts;
    public uint GetArrowCounts(Arrow arrow)
    {
        return m_arrowCounts.TryGetValue(arrow, out uint count) ? count : (uint)0;
    }
}