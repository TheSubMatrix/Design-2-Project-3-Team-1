using AudioSystem;
using UnityEngine;

public class BounceArrow : Arrow
{
    bool m_isBouncy;
    [SerializeField] SoundData m_bounceSound;
    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        if (!CompletedTrajectory)
        {
            OnImpact(collision);
            CompletedTrajectory = true;
        }
        if (!m_isBouncy || collision.rigidbody == null || collision.rigidbody.bodyType == RigidbodyType2D.Static) return;
        collision.rigidbody.linearVelocity = Vector2.up * 10;
        SoundManager.Instance.CreateSound().WithSoundData(m_bounceSound).WithPosition(transform.position).WithRandomPitch().Play();
    }

    protected override void OnEmbed(Collision2D collision)
    {
        base.OnEmbed(collision);
        m_isBouncy = true;
    }
}