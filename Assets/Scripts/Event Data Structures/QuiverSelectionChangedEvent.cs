public struct QuiverSelectionChangedEvent : IEvent
{
    public QuiverSelectionChangedEvent(string selected)
    {
        Selected = selected;
    }

    public string Selected { get; }
}