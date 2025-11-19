using System.Collections.Generic;
using UnityEngine;

public interface ILevelDataProvider
{
    Dictionary<Arrow, uint> GetArrowCounts();
    string GetNextLevel();
}
