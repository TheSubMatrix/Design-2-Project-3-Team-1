using System;
using UnityEngine;

[Serializable]
public class BowUIData
{
    string m_arrowTypeName;
    Sprite m_arrowUISprite;
    uint m_currentAmmo;

    public string ArrowTypeName
    {
        get => m_arrowTypeName;
        set => m_arrowTypeName = value;
    }

    public Sprite ArrowUISprite
    {
        get => m_arrowUISprite;
        set => m_arrowUISprite = value;
    }

    public uint CurrentAmmo
    {
        get => m_currentAmmo;
        set => m_currentAmmo = value;
    }
    public BowUIData WithAmmo(uint ammo)
    {
        CurrentAmmo = ammo;
        return this;
    }

    public BowUIData WithArrowTypeName(string arrowType)
    {
        ArrowTypeName = arrowType;
        return this;
    }

    public BowUIData WithArrowUISprite(Sprite sprite)
    {
        ArrowUISprite = sprite;
        return this;
    }
}