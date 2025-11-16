using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace VFXSystem
{
    [Serializable]
    public class VFXData
    {
        public VisualEffectAsset Asset;
        public bool Loop;
        public bool PlayedFrequently;
        [ClassSelector, SerializeReference] public List<VFXProperty> Properties = new();
    }
}