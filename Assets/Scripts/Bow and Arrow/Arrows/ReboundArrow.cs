using System.Linq;
using AudioSystem;
using UnityEngine;

public class ReboundArrow : Arrow
{
    bool m_hasBounced;
    
    [Header("Bounce Settings")]
    [SerializeField] [Range(0f, 1.5f)] float m_bounciness = 0.8f; 
    [SerializeField] float m_minBounceSpeed = 5f; // New: Prevent weak bounces
    [SerializeField] SoundData m_bounceSound;
    

    public override void Fire(Vector2 direction, float powerPercentage, Collider2D playerCollider = null)
    {
        m_hasBounced = false;
        base.Fire(direction, powerPercentage, playerCollider);
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        if (CompletedTrajectory || !IsValidCollision(collision)) return;
        if (m_hasBounced)
        {
            OnImpact(collision);
            return;
        }
        bool isStuckArrow = LayerMask.LayerToName(collision.gameObject.layer) == "Arrow" && 
                            collision.gameObject.GetComponent<Arrow>()?.StuckInWall == true;
        if (!isStuckArrow)
        {
            PerformManualBounce(collision);
        }
        else
        {
            OnImpact(collision);
        }
    }

    void PerformManualBounce(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent(out IDamageable damageable))
        {
            damageable.Damage(100);
        }
        Vector2 contactNormal = collision.contacts.Aggregate(Vector2.zero, (current, contact) => current + contact.normal);
        contactNormal.Normalize();
        Vector2 incomingVelocity = -collision.relativeVelocity;
        Vector2 reflectedVelocity = Vector2.Reflect(incomingVelocity, contactNormal);
        Vector2 finalVelocity = reflectedVelocity * m_bounciness;
        if (finalVelocity.magnitude < m_minBounceSpeed)
        {
            finalVelocity = finalVelocity.normalized * m_minBounceSpeed;
        }
        
        RB.linearVelocity = finalVelocity;
        RB.position += contactNormal * 0.05f;

        // E. Update Rotation
        float angle = Mathf.Atan2(RB.linearVelocity.y, RB.linearVelocity.x) * Mathf.Rad2Deg;
        RB.rotation = angle;
        m_hasBounced = true;
        RB.angularVelocity = 0f;
        SoundManager.Instance.CreateSound().WithSoundData(m_bounceSound).WithRandomPitch().WithPosition(transform.position).Play();
    }
}