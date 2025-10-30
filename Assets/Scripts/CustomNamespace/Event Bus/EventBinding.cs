using System;

public interface IEventBinding<T>
{
    public Action<T> OnEvent { get; set; }
    public Action OnEventNoArgs { get; set; }
}

public partial class EventBinding<T> : IEventBinding<T> where T : IEvent
{
    Action<T> m_onEvent = _ => { };
    Action m_onEventNoArgs = () => { };

    public Action<T> OnEvent
    {
        get => m_onEvent;
        set => m_onEvent = value;
    }

    public Action OnEventNoArgs
    {
        get => m_onEventNoArgs;
        set => m_onEventNoArgs = value;
    }
    public EventBinding(Action<T> onEvent) => m_onEvent = onEvent;
    public EventBinding(Action onEvent) => m_onEventNoArgs = onEvent;
    public void Add(Action onEvent) => m_onEventNoArgs += onEvent;
    public void Add(Action<T> onEvent) => m_onEvent += onEvent;
    public void Remove(Action onEvent) => m_onEventNoArgs -= onEvent;
    public void Remove(Action<T> onEvent) => m_onEvent -= onEvent;
    
}