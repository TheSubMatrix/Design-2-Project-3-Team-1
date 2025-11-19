public struct QuiverSelectionChangedEvent : IEvent
{
    public QuiverSelectionChangedEvent(Arrow selected)
    {
        Selected = selected;
    }

    public Arrow Selected { get; }
}