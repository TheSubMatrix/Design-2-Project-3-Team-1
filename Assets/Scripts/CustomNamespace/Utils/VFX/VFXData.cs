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
        
        [Header("Properties")]
        public List<VFXProperty> Properties = new();

        public VFXData WithProperty(string name, object value)
        {
            Properties.Add(new VFXProperty(name, value));
            return this;
        }
    }
}