using System;
using UnityEngine;

[Serializable]
public class BowUIData
{



    public string ArrowTypeName { get; set; }
    public Sprite ArrowUISprite { get; set; }
    public uint CurrentAmmo { get; set; }
    
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