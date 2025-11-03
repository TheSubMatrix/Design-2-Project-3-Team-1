using CustomNamespace.GenericDatatypes;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] Observer<uint> m_health;
    [SerializeField] Observer<uint> m_maxHealth = 100;
    [SerializeField] Observer<bool> m_isDead;
    [SerializeField] Observer<bool> m_isInvulnerable;
    void Start()
    {
        m_health = m_maxHealth.Value;
    }

    public void Damage(uint amount)
    {
        if(m_isInvulnerable || m_isDead) return;
        m_health = m_health > amount ? m_health - amount : 0;
        if(m_health == 0)
        {
            m_isDead = true;
        }
    }

    public void Heal(uint amount)
    {
        m_health = m_health + amount > m_maxHealth ? m_maxHealth : m_health + amount;
        if(m_isDead)
            m_isDead = false;
    }
}
