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
        if (CompletedTrajectory) return;
        if (!m_hasBounced && collision.gameObject.layer != LayerMask.NameToLayer("Arrow Surface"))
            OnBounce();
        else
            OnImpact(collision);
    }

    protected override void OnImpact(Collision2D collision)
    {
        RB.sharedMaterial = m_standardPhysicsMaterial;
        base.OnImpact(collision);
    }

    protected override void OnEmbed()
    {
        RB.linearVelocity = Vector2.zero;
        RB.angularVelocity = 0f;
        base.OnEmbed();
    }

    void OnBounce()
    {
        m_hasBounced = true;
        RB.angularVelocity = 0f; // Reset angular velocity after bounce
        SoundManager.Instance.CreateSound().WithSoundData(m_bounceSound).WithRandomPitch().WithPosition(transform.position).Play();
    }
}