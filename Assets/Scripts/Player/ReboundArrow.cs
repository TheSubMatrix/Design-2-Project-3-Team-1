using UnityEngine;

public class ReboundArrow : Arrow
{
    bool m_hasBounced;
    [SerializeField] PhysicsMaterial2D m_bouncePhysicsMaterial;
    [SerializeField] PhysicsMaterial2D m_standardPhysicsMaterial;
    
    public override void Fire(Vector2 direction, float powerPercentage, Collider2D playerCollider = null)
    {
        m_hasBounced = false;
        RB.sharedMaterial = m_bouncePhysicsMaterial;
        base.Fire(direction, powerPercentage, playerCollider);
    }
    
    protected override void OnImpact(Collision2D collision)
    {
        if (collision.gameObject.layer.Equals(LayerMask.NameToLayer("Arrow Surface")))
        {
            StuckInWall = true;
            RB.bodyType = RigidbodyType2D.Static;
            RB.useFullKinematicContacts = true;
            CompletedTrajectory = true;
        }
        else if (!m_hasBounced)
        {
            m_hasBounced = true;
            RB.sharedMaterial = m_standardPhysicsMaterial;
            RB.angularVelocity = 0f; // Reset angular velocity after bounce
        }
        else
        {
            CompletedTrajectory = true;
        }
    }
}