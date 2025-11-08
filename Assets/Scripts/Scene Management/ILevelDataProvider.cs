using UnityEngine;

public interface ILevelDataProvider
{
    uint GetArrowCounts(Arrow arrow);
    string GetNextLevel();
}
