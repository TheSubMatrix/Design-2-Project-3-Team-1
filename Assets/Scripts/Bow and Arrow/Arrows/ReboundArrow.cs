using AudioSystem;
using UnityEngine;

public class ReboundArrow : Arrow
{
    bool m_hasBounced;
    [SerializeField] PhysicsMaterial2D m_bouncePhysicsMaterial;
    [SerializeField] PhysicsMaterial2D m_standardPhysicsMaterial;
    [SerializeField] SoundData m_bounceSound;
    public override void Fire(Vector2 direction, float powerPercentage, Collider2D playerCollider = null)
    {
        m_hasBounced = false;
        RB.sharedMaterial = m_bouncePhysicsMaterial;
        base.Fire(direction, powerPercentage, playerCollider);
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        switch (m_hasBounced)
        {
            case false:
                OnBounce();
                break;
            case true:
                OnImpact(collision);
                break;
        }
    }

    void OnBounce()
    {
        m_hasBounced = true;
        RB.sharedMaterial = m_standardPhysicsMaterial;
        RB.angularVelocity = 0f; // Reset angular velocity after bounce
        SoundManager.Instance.CreateSound().WithSoundData(m_bounceSound).WithRandomPitch().WithPosition(transform.position).Play();
    }
}