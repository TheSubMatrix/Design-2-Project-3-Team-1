using System.Collections.Generic;
using UnityEngine;

public static class EventBus<T> where T : IEvent
{
    static readonly HashSet<IEventBinding<T>> Bindings = new HashSet<IEventBinding<T>>();
    public static void Register(IEventBinding<T> binding) => Bindings.Add(binding);
    public static void Deregister(IEventBinding<T> binding) => Bindings.Remove(binding);
    public static void Raise(T @event)
    {
        foreach (IEventBinding<T> binding in Bindings)
        {
            binding.OnEvent.Invoke(@event);
            binding.OnEventNoArgs.Invoke();
        }
    }

    static void Clear()
    {
        Debug.Log($"Clearing all bindings for {typeof(T).Name}");
        Bindings.Clear();
    }
}