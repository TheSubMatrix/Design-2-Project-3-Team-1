using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace AudioSystem
{
    public class SoundManager : PersistentSingleton<SoundManager>
    {
        IObjectPool<SoundEmitter> m_soundEmitterPool;
        public readonly List<SoundEmitter> m_activeEmitters = new();
        public readonly Queue<SoundEmitter> FrequentEmitters = new();
        
        [SerializeField] SoundEmitter m_soundEmitterPrefab;
        [SerializeField] bool m_collectionCheck = true;
        [SerializeField] int m_initialPoolSize = 10;
        [SerializeField] int m_maxPoolSize = 100;
        [SerializeField] int m_maxSoundInstances = 30;

        public SoundBuilder CreateSound() => new(this);
        public SoundEmitter Get() => m_soundEmitterPool.Get();
        public void ReturnToPool(SoundEmitter soundEmitter) => m_soundEmitterPool.Release(soundEmitter);

        public bool CanPlaySound(SoundData soundData)
        {
            if (!soundData.PlayedFrequently) return true;
            if (FrequentEmitters.Count < m_maxSoundInstances || !FrequentEmitters.TryDequeue(out SoundEmitter emitter)) return true;
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

        protected override void InitializeSingleton()
        {
            base.InitializeSingleton();
            InitializePool();
        }
        SoundEmitter CreateSoundEmitter()
        {
           SoundEmitter emitter = Instantiate(m_soundEmitterPrefab, transform, true);
           emitter.gameObject.SetActive(false);
           return emitter;
        }
        void OnTakeFromPool(SoundEmitter emitter)
        {
            emitter.gameObject.SetActive(true);
            m_activeEmitters.Add(emitter);
        }
        void OnReturnedToPool(SoundEmitter emitter)
        {
            emitter.gameObject.SetActive(false);
            m_activeEmitters.Remove(emitter);
        }

        void OnDestroyPooledObject(SoundEmitter emitter)
        {
            Destroy(emitter.gameObject);
        }
        void InitializePool()
        {
            m_soundEmitterPool = new ObjectPool<SoundEmitter>
            (
                CreateSoundEmitter,
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyPooledObject,
                m_collectionCheck,
                m_initialPoolSize,
                m_maxPoolSize
            );
        }
    }
}
