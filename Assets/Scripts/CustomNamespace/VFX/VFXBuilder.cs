using UnityEngine;
using System.Collections.Generic;
using UnityEngine.VFX;

namespace VFXSystem
{
    public class VFXBuilder
    {
        readonly VFXManager m_vfxManager;
        VFXData m_vfxData;
        Vector3 m_position = Vector3.zero;
        Quaternion m_rotation = Quaternion.identity;
        Transform m_transformToFollow;
        bool m_followRotation;
        Vector3? m_scale;

        public VFXBuilder(VFXManager vfxManager)
        {
            m_vfxManager = vfxManager;
        }
        public VFXBuilder WithVFXData(VFXData vfxData)
        {
            m_vfxData = vfxData;
            return this;
        }

        public VFXBuilder WithPosition(Vector3 position)
        {
            m_position = position;
            return this;
        }

        public VFXBuilder WithRotation(Quaternion rotation)
        {
            m_rotation = rotation;
            return this;
        }
        
        public VFXBuilder WithRotation(Vector3 eulerAngles)
        {
            m_rotation = Quaternion.Euler(eulerAngles);
            return this;
        }

        public VFXBuilder WithScale(Vector3 scale)
        {
            m_scale = scale;
            return this;
        }

        public VFXBuilder WithScale(float uniformScale)
        {
            m_scale = Vector3.one * uniformScale;
            return this;
        }
        
        public VFXBuilder AttachedTo(Transform attachmentTransform, bool followRotation = false)
        {
            m_transformToFollow = attachmentTransform;
            m_followRotation = followRotation;

            if (m_transformToFollow == null) return this;
            m_position = m_transformToFollow.position;
            if (followRotation)
            {
                m_rotation = m_transformToFollow.rotation;
            }

            return this;
        }
        
        public VFXEmitter Play()
        {
            VFXEmitter emitter = SetupVFX();
            emitter?.Play();
            return emitter;
        }

        public VFXEmitter InvokeEvent(string eventName)
        {
            VFXEmitter emitter = SetupVFX();
            emitter?.InvokeEvent(eventName);
            return emitter;
        }

        VFXEmitter SetupVFX()
        {
            if (m_vfxData == null || m_vfxData.Asset == null)
            {
                Debug.LogError("Cannot play VFX: VFXData or Asset is null");
                return null;
            }

            if (!m_vfxManager.CanPlayVFX(m_vfxData))
            {
                return null;
            }

            VFXEmitter emitter = m_vfxManager.Get(m_vfxData.Asset);
            if (emitter == null)
            {
                Debug.LogError("Failed to get VFX emitter from pool");
                return null;
            }

            emitter.Initialize(m_vfxData); 
            emitter.transform.position = m_position;
            emitter.transform.rotation = m_rotation;
            
            if (m_scale.HasValue)
            {
                emitter.transform.localScale = m_scale.Value;
            }
            emitter.SetTransformToFollow(m_transformToFollow, m_followRotation);
            if (m_vfxData.PlayedFrequently)
            {
                m_vfxManager.FrequentEmitters.Enqueue(emitter);
            }
            return emitter;
        }
    }
}