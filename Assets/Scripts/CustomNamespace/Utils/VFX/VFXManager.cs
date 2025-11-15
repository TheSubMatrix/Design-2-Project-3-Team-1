using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.VFX;

namespace VFXSystem
{
    public class VFXManager : PersistentSingleton<VFXManager>
    {
        [SerializeField] VFXEmitter m_vfxEmitterPrefab;
        [SerializeField] bool m_collectionCheck = true;
        [SerializeField] int m_initialPoolSize = 5;
        [SerializeField] int m_maxPoolSize = 50;
        [SerializeField] int m_maxVFXInstances = 30;

        [SerializeField] SerializableDictionary<VisualEffectAsset, VFXPoolData> m_vfxPools = new();
        
        public readonly List<VFXEmitter> m_activeEmitters = new();
        public readonly Queue<VFXEmitter> FrequentEmitters = new();

        [Serializable]
        public class VFXPoolData
        {
            public IObjectPool<VFXEmitter> Pool;
            public int InitialSize = 5;
            public int MaxSize = 50;
            
            [NonSerialized]
            public bool IsInitialized;
        }

        public VFXBuilder CreateVFX() => new(this);
        
        public VFXEmitter Get(VisualEffectAsset asset)
        {
            if (asset == null)
            {
                Debug.LogError("Cannot get VFX emitter for null asset");
                return null;
            }

            if (!m_vfxPools.ContainsKey(asset))
            {
                CreatePoolForAsset(asset);
            }

            VFXPoolData poolData = m_vfxPools[asset];
            if (!poolData.IsInitialized)
            {
                InitializePool(asset, poolData);
            }

            return poolData.Pool.Get();
        }

        public void ReturnToPool(VFXEmitter vfxEmitter, VisualEffectAsset asset)
        {
            if (!asset || !m_vfxPools.ContainsKey(asset))
            {
                Debug.LogWarning("Attempted to return VFX emitter to non-existent pool");
                Destroy(vfxEmitter.gameObject);
                return;
            }

            m_vfxPools[asset].Pool.Release(vfxEmitter);
        }

        public bool CanPlayVFX(VFXData vfxData)
        {
            if (vfxData is not null && !vfxData.PlayedFrequently) return true;
            if (FrequentEmitters.Count < m_maxVFXInstances || !FrequentEmitters.TryDequeue(out VFXEmitter emitter)) return true;
            
            try
            {
                emitter.Stop();
                return true;
            }
            catch
            {
                Debug.Log("Emitter already released");
            }

            return false;
        }

        void CreatePoolForAsset(VisualEffectAsset asset)
        {
            VFXPoolData poolData = new()
            {
                InitialSize = m_initialPoolSize,
                MaxSize = m_maxPoolSize
            };
            
            m_vfxPools[asset] = poolData;
            InitializePool(asset, poolData);
        }

        void InitializePool(VisualEffectAsset asset, VFXPoolData poolData)
        {
            poolData.Pool = new ObjectPool<VFXEmitter>
            (
                () => CreateVFXEmitter(asset),
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyPooledObject,
                m_collectionCheck,
                poolData.InitialSize,
                poolData.MaxSize
            );
            
            poolData.IsInitialized = true;
        }

        VFXEmitter CreateVFXEmitter(VisualEffectAsset asset)
        {
            VFXEmitter emitter = Instantiate(m_vfxEmitterPrefab, transform, true);
            emitter.gameObject.SetActive(false);
            emitter.SetVisualEffectAsset(asset);
            return emitter;
        }

        void OnTakeFromPool(VFXEmitter emitter)
        {
            emitter.gameObject.SetActive(true);
            m_activeEmitters.Add(emitter);
        }

        void OnReturnedToPool(VFXEmitter emitter)
        {
            emitter.gameObject.SetActive(false);
            m_activeEmitters.Remove(emitter);
        }

        static void OnDestroyPooledObject(VFXEmitter emitter)
        {
            Destroy(emitter.gameObject);
        }

        protected override void InitializeSingleton()
        {
            base.InitializeSingleton();
            
            // Initialize any pre-configured pools
            foreach (KeyValuePair<VisualEffectAsset, VFXPoolData> kvp in m_vfxPools)
            {
                if (kvp.Key != null && !kvp.Value.IsInitialized)
                {
                    InitializePool(kvp.Key, kvp.Value);
                }
            }
        }

#if UNITY_EDITOR
        // Helper method for the editor to pre-configure pools
        public void ConfigurePool(VisualEffectAsset asset, int initialSize, int maxSize)
        {
            if (asset == null) return;

            if (!m_vfxPools.ContainsKey(asset))
            {
                m_vfxPools[asset] = new VFXPoolData
                {
                    InitialSize = initialSize,
                    MaxSize = maxSize
                };
            }
            else
            {
                m_vfxPools[asset].InitialSize = initialSize;
                m_vfxPools[asset].MaxSize = maxSize;
            }
        }
#endif
    }
}
