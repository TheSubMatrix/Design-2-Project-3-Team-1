using System;
using System.Collections;
using CustomNamespace.Extensions;
using UnityEngine;
namespace AudioSystem
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter : MonoBehaviour
    {
        public SoundData SoundData { get; private set; }
        AudioSource m_audioSource;
        Coroutine m_playSoundCoroutine;
        Transform m_cachedTransform;
        Transform m_transformToFollow;
        Vector3 m_cachedPosition;
        
        void Awake()
        {
            m_audioSource = gameObject.GetOrAddComponent<AudioSource>();
            m_cachedTransform = transform;
        }
        void Update()
        {
            if (!m_transformToFollow || m_cachedPosition == m_transformToFollow.position) return;
            m_cachedTransform.position = m_transformToFollow.position;
            m_cachedPosition = m_transformToFollow.position;
        }

        public void SetTransformToFollow(Transform transformToFollow)
        {
            m_transformToFollow = transformToFollow;
            if (!m_transformToFollow) return;
            m_cachedPosition = transformToFollow.position;
        }
        
        public void Play()
        {
            if (m_playSoundCoroutine is not null)
            {
                StopCoroutine(m_playSoundCoroutine);
            }
            m_audioSource.Play();
            m_playSoundCoroutine = StartCoroutine(WaitForSoundToFinishAsync());
        }
        public void Stop()
        {
            if (m_playSoundCoroutine is not null)
            {
                StopCoroutine(m_playSoundCoroutine);
                m_playSoundCoroutine = null;
            }
            m_audioSource.Stop();
            SoundManager.Instance.ReturnToPool(this);
        }
        IEnumerator WaitForSoundToFinishAsync()
        {
            yield return new WaitWhile(() => m_audioSource.isPlaying);
            SoundManager.Instance.ReturnToPool(this);
        }
        public void WithRandomPitch(float minPitchShift = -0.05f, float maxPitchShift = 0.05f)
        {
            m_audioSource.pitch += UnityEngine.Random.Range(minPitchShift, maxPitchShift);
        }
        public void Initialize(SoundData soundData)
        {
            SoundData = soundData;
            m_audioSource.clip = soundData.Clip;
            m_audioSource.outputAudioMixerGroup = soundData.MixerGroup;
            m_audioSource.loop = soundData.Loop;
            m_audioSource.playOnAwake = soundData.PlayOnAwake;
            m_audioSource.volume = soundData.Volume;
            m_audioSource.pitch = soundData.Pitch;
            m_audioSource.panStereo = soundData.PanStereo;
            m_audioSource.spatialBlend = soundData.SpatialBlend;
            m_audioSource.reverbZoneMix = soundData.ReverbZoneMix;
            m_audioSource.dopplerLevel = soundData.DopplerLevel;
            m_audioSource.spread = soundData.Spread;
            m_audioSource.minDistance = soundData.MinDistance;
            m_audioSource.maxDistance = soundData.MaxDistance;
            m_audioSource.ignoreListenerVolume = soundData.IgnoreListenerVolume;
            m_audioSource.ignoreListenerPause = soundData.IgnoreListenerPause;
            m_audioSource.rolloffMode = soundData.RolloffMode;
        }
    }

}
