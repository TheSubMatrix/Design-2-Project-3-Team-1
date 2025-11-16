using System;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(BoxCollider2D), typeof(VisualEffect), typeof(AreaEffector2D)), ExecuteInEditMode]
public class WindArea : MonoBehaviour
{
    BoxCollider2D m_collider;
    VisualEffect m_windEffect;
    AreaEffector2D m_windEffector;
    [SerializeField, Range(0,1)]float m_particleSpeedScale = 1f;
    void Awake()
    {
        m_collider ??= GetComponent<BoxCollider2D>();
        m_windEffect ??= GetComponent<VisualEffect>();
        m_windEffector ??= GetComponent<AreaEffector2D>();
    }
    void Update()
    {
        if(Application.isPlaying) return;
        if(!m_windEffect || !m_collider || !m_windEffector) return;
        m_windEffect.SetVector2("Box Size", m_collider.size);
        m_windEffect.SetVector2("Box Offset", m_collider.offset);
        Vector2 windVectorLocal = new Vector2(Mathf.Cos(m_windEffector.forceAngle * Mathf.Deg2Rad), Mathf.Sin(m_windEffector.forceAngle * Mathf.Deg2Rad)).normalized;
        Vector2 windVectorWorld = transform.InverseTransformVector(windVectorLocal);
        m_windEffect.SetVector2("Wind Vector", m_windEffector.useGlobalAngle ? windVectorWorld : windVectorLocal);
        m_windEffect.SetFloat("Speed Normal", m_windEffector.forceMagnitude * m_particleSpeedScale);
        m_windEffect.SetFloat("Speed Varied", (m_windEffector.forceMagnitude + m_windEffector.forceVariation) * m_particleSpeedScale);
    }
}
