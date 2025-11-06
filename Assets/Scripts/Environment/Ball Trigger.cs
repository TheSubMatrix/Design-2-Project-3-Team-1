using System;
using CustomNamespace.GenericDatatypes;
using UnityEngine;
using UnityEngine.Events;
[RequireComponent(typeof(Collider2D))]
public class BallTrigger : MonoBehaviour
{
    [SerializeField] LayerMask m_triggerLayers;
    [SerializeField] Observer<bool> m_isTriggered;
    void OnTriggerEnter2D(Collider2D other)
    {
        if((m_triggerLayers & other.gameObject.layer) is 0){ return; }
        m_isTriggered.Value = true;
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if((m_triggerLayers & other.gameObject.layer) is 0){ return; }
        m_isTriggered.Value = false;
    }
}
