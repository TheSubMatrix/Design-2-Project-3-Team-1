using System.Collections;
using System.Reflection;
using CustomNamespace.Extensions;
using UnityEngine;
using UnityEngine.VFX;

namespace VFXSystem
{
    [RequireComponent(typeof(VisualEffect))]
    public class VFXEmitter : MonoBehaviour
    {
        public VFXData VFXData { get; private set; }
        public VisualEffectAsset Asset { get; private set; }
        
        VisualEffect m_visualEffect;
        Coroutine m_playVFXCoroutine;
        Transform m_cachedTransform;
        Transform m_transformToFollow;
        Vector3 m_cachedPosition;
        Quaternion m_cachedRotation;
        bool m_followRotation;

        void Awake()
        {
            m_visualEffect = gameObject.GetOrAddComponent<VisualEffect>();
            m_cachedTransform = transform;
        }

        void Update()
        {
            if (!m_transformToFollow) return;

            bool positionChanged = m_cachedPosition != m_transformToFollow.position;
            bool rotationChanged = m_followRotation && m_cachedRotation != m_transformToFollow.rotation;

            switch (positionChanged)
            {
                case false when !rotationChanged:
                    return;
                case true:
                    m_cachedTransform.position = m_transformToFollow.position;
                    m_cachedPosition = m_transformToFollow.position;
                    break;
            }

            if (!rotationChanged) return;
            m_cachedTransform.rotation = m_transformToFollow.rotation;
            m_cachedRotation = m_transformToFollow.rotation;
        }

        public void SetVisualEffectAsset(VisualEffectAsset asset)
        {
            Asset = asset;
            if (m_visualEffect != null)
            {
                m_visualEffect.visualEffectAsset = asset;
            }
        }

        public void SetTransformToFollow(Transform transformToFollow, bool followRotation = false)
        {
            m_transformToFollow = transformToFollow;
            m_followRotation = followRotation;
            
            if (!m_transformToFollow) return;
            
            m_cachedPosition = transformToFollow.position;
            if (followRotation)
            {
                m_cachedRotation = transformToFollow.rotation;
            }
        }

        public void InvokeEvent(string eventName)
        {
            if (m_playVFXCoroutine != null)
            {
                StopCoroutine(m_playVFXCoroutine);
            }
            m_visualEffect.SendEvent(eventName);
            if (!VFXData.Loop)
            {
                m_playVFXCoroutine = StartCoroutine(WaitForVFXToFinishAsync());
            }
        }
        public void Play()
        {
            if (m_playVFXCoroutine != null)
            {
                StopCoroutine(m_playVFXCoroutine);
            }
            
            m_visualEffect.Play();
            
            if (!VFXData.Loop)
            {
                m_playVFXCoroutine = StartCoroutine(WaitForVFXToFinishAsync());
            }
        }

        public void Stop()
        {
            if (m_playVFXCoroutine != null)
            {
                StopCoroutine(m_playVFXCoroutine);
                m_playVFXCoroutine = null;
            }
            
            m_visualEffect.Stop();
            VFXManager.Instance.ReturnToPool(this, Asset);
        }

        IEnumerator WaitForVFXToFinishAsync()
        {
            // Wait for the effect to finish playing
            yield return new WaitWhile(() => m_visualEffect.aliveParticleCount > 0);
            
            // Add a small buffer to ensure everything is cleaned up
            yield return new WaitForSeconds(0.1f);
            
            VFXManager.Instance.ReturnToPool(this, Asset);
        }

        public void Initialize(VFXData vfxData)
        {
            VFXData = vfxData;
            
            if (vfxData.Asset != null && m_visualEffect.visualEffectAsset != vfxData.Asset)
            {
                m_visualEffect.visualEffectAsset = vfxData.Asset;
                Asset = vfxData.Asset;
            }

            // Apply all properties dynamically
            foreach (VFXProperty property in vfxData.Properties)
            {
                property.ApplyToVisualEffect(m_visualEffect);
            }

            // Reset effect state
            m_visualEffect.Reinit();
        }

        // Generic property setter for runtime modifications
        public void SetProperty(string propertyName, object value)
        {
            VFXProperty property = new VFXProperty(propertyName, value);
            property.ApplyToVisualEffect(m_visualEffect);
        }

        // Get property value if needed
        public T GetProperty<T>(string propertyName)
        {
            System.Type valueType = typeof(T);
            
            // Use reflection to find the appropriate Get method
            MethodInfo[] methods = typeof(VisualEffect).GetMethods(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (MethodInfo method in methods)
            {
                if (!method.Name.StartsWith("Get")) continue;
                
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string)) continue;

                if (method.ReturnType != valueType && !method.ReturnType.IsAssignableFrom(valueType)) continue;
                try
                {
                    return (T)method.Invoke(m_visualEffect, new object[] { propertyName });
                }
                catch
                {
                    // Continue to next method
                }
            }
            
            return default;
        }
    }
}