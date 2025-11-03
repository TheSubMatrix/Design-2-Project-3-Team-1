using UnityEngine;

public class BounceArrow : Arrow
{
    bool m_isBouncy;
    protected override void OnImpact(Collision2D collision)
    {
        if (collision.gameObject.layer.Equals(LayerMask.NameToLayer("Arrow Surface")))
        {
            StuckInWall = true;
            RB.bodyType = RigidbodyType2D.Static;
            RB.useFullKinematicContacts = true;
            m_isBouncy = true;
        }
        if (m_isBouncy)
        {
            if (collision.rigidbody != null && collision.rigidbody.bodyType != RigidbodyType2D.Static)
            {
                collision.rigidbody.linearVelocity = Vector2.up * 10;
            }
        }
    }


}