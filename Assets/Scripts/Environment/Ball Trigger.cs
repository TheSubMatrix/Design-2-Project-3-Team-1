using System;
using AudioSystem;
using CustomNamespace.GenericDatatypes;
using UnityEngine;
using UnityEngine.Events;
[RequireComponent(typeof(Collider2D))]
public class BallTrigger : MonoBehaviour
{
    [SerializeField] LayerMask m_triggerLayers;
    [SerializeField] Observer<bool> m_isTriggered;
    [SerializeField] SoundData m_triggerOn;
    [SerializeField] SoundData m_triggerOff;
    void OnTriggerEnter2D(Collider2D other)
    {
        if((m_triggerLayers.value & (1 << other.gameObject.layer)) is 0){ return; }
        SoundManager.Instance.CreateSound().WithSoundData(m_triggerOn).WithRandomPitch().WithPosition(transform.position).Play();
        m_isTriggered.Value = true;
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if((m_triggerLayers.value & (1 << other.gameObject.layer)) is 0){ return; }
        SoundManager.Instance.CreateSound().WithSoundData(m_triggerOff).WithRandomPitch().WithPosition(transform.position).Play();
        m_isTriggered.Value = false;
    }
}
