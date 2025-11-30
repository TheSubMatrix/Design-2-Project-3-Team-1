using UnityEngine;
using UnityEngine.Events;

public interface IGoalEventProvider
{
    UnityEvent<bool> OnGoalStateChanged { get; } 
}
