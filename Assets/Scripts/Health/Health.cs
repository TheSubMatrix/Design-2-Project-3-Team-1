using AudioSystem;
using CustomNamespace.GenericDatatypes;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] Observer<uint> m_health;
    [SerializeField] Observer<uint> m_maxHealth = new Observer<uint>(100);
    [SerializeField] Observer<bool> m_isDead;
    [SerializeField] Observer<bool> m_isInvulnerable;
    [SerializeField] SoundData m_deathSound;
    [SerializeField] SoundData m_damageSound;
    void Start()
    {
        m_health.Value = m_maxHealth.Value;
    }

    public void Damage(uint amount)
    {
        if(m_isInvulnerable.Value || m_isDead.Value) return;
        
        m_health.Value = m_health.Value > amount ? m_health.Value - amount : 0;

        if (m_health.Value <= 0)
        {
            m_isDead.Value = true;
            SoundManager.Instance.CreateSound().WithSoundData(m_deathSound).WithRandomPitch()
                .WithPosition(transform.position).Play();
        }
        else
        {
            SoundManager.Instance.CreateSound().WithSoundData(m_damageSound).WithRandomPitch()
                .WithPosition(transform.position).Play();
        }
    }

    public void Heal(uint amount)
    {
        m_health.Value = m_health.Value + amount > m_maxHealth.Value ? m_maxHealth.Value : m_health.Value + amount;
        if(m_isDead.Value)
            m_isDead.Value = false;
    }
}
