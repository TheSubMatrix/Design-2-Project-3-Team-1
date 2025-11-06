using System;
using UnityEngine;

public class Spike : MonoBehaviour
{
    [SerializeField]uint m_damage = 100;
    void OnCollisionEnter2D(Collision2D other)
    {
        if(!other.gameObject.TryGetComponent(out IDamageable health)){ return;}
        health.Damage(m_damage);
    }
}
