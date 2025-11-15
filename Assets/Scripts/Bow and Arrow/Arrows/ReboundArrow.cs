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
        
        bool isArrowSurface = collision.gameObject.layer == LayerMask.NameToLayer("Arrow Surface");
        bool isStuckArrow = LayerMask.LayerToName(collision.gameObject.layer) == "Arrow" && 
                            collision.gameObject.GetComponent<Arrow>()?.StuckInWall == true;
        
        // If hasn't bounced yet and not hitting a surface/stuck arrow, bounce instead
        if (!m_hasBounced && !isArrowSurface && !isStuckArrow)
        {
            OnBounce(collision);
        }
        else
        {
            OnImpact(collision);
        }
    }

    protected override void OnImpact(Collision2D collision)
    {
        // Stop movement immediately when impacting after bounce
        RB.linearVelocity = Vector2.zero;
        RB.angularVelocity = 0f;
        RB.sharedMaterial = m_standardPhysicsMaterial;
        base.OnImpact(collision);
    }

    protected override void OnEmbed(Collision2D collision)
    {
        // Stop all movement immediately before calling base
        RB.linearVelocity = Vector2.zero;
        RB.angularVelocity = 0f;
        RB.sharedMaterial = m_standardPhysicsMaterial;
        base.OnEmbed(collision);
    }

    void OnBounce(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent(out IDamageable damageable))
        {
            damageable.Damage(100);
        }
        m_hasBounced = true;
        RB.sharedMaterial = m_standardPhysicsMaterial;
        RB.angularVelocity = 0f; // Reset angular velocity after bounce
        SoundManager.Instance.CreateSound().WithSoundData(m_bounceSound).WithRandomPitch().WithPosition(transform.position).Play();
    }
}