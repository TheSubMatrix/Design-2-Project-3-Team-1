using System;
using UnityEngine;

public struct QuiverUpdatedEvent : IEvent, IEquatable<QuiverUpdatedEvent>
{
    public uint ArrowCount { get; }
    public Sprite Sprite { get; }
    public Arrow TrackedArrow { get; set; }
    
    public QuiverUpdatedEvent(uint arrowCount, Sprite sprite, Arrow arrow)
    {
        ArrowCount = arrowCount;
        Sprite = sprite;
        TrackedArrow = arrow;
    }
    public bool Equals(QuiverUpdatedEvent other)
    {
        return ArrowCount == other.ArrowCount && Equals(Sprite, other.Sprite) && Equals(TrackedArrow, other.TrackedArrow);
    }
    public override bool Equals(object obj)
    {
        return obj is QuiverUpdatedEvent other && Equals(other);
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(ArrowCount, Sprite, TrackedArrow);
    }
}