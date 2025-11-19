using UnityEngine;

public struct QuiverUpdatedEvent : IEvent
{
    public uint Arrows { get; }
    public Sprite Sprite { get; }
    public string Name { get; }
    public QuiverUpdatedEvent(uint arrows, Sprite sprite, string name)
    {
        Arrows = arrows;
        Sprite = sprite;
        Name = name;
    }
}