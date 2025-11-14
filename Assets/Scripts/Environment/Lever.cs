using System;
using AudioSystem;
using CustomNamespace.GenericDatatypes;
using UnityEngine;
[RequireComponent(typeof(HingeJoint2D), typeof(Rigidbody2D))]
public class Lever : MonoBehaviour
{
    [SerializeField] Observer<bool> m_isTriggered;
    [SerializeField] SoundData m_triggerSound;
    
    HingeJoint2D m_hingeJoint;
    void Awake()
    {
        m_hingeJoint = GetComponent<HingeJoint2D>();
        
    }
    void FixedUpdate()
    {
        bool newTriggeredState = m_hingeJoint.jointAngle > ((m_hingeJoint.limits.max + m_hingeJoint.limits.min) / 2);
        if (m_isTriggered.Value == newTriggeredState) return;
        SoundManager.Instance.CreateSound().WithSoundData(m_triggerSound).WithRandomPitch().WithPosition(transform.position).Play();
        m_isTriggered.Value = newTriggeredState;
        Debug.Log(m_isTriggered.Value);
    }
    
}
