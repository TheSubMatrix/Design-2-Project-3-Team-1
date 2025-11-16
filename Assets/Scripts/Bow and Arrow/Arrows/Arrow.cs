using System.Collections.Generic;
using AudioSystem;
using UnityEngine;
using UnityEngine.Serialization;
using VFXSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class Arrow : MonoBehaviour
{
    /// <summary>
    /// The current Rigidbody2D component of the arrow.
    /// </summary>
    public Rigidbody2D RB { get; private set; }
    public bool StuckInWall { get; protected set; }
    [SerializeField] float m_fireForce = 10f;
    [SerializeField] uint m_trajectoryPointCount = 20;
    [SerializeField] float m_trajectoryPointTime = 0.1f;
    [FormerlySerializedAs("m_collisionMask")] [SerializeField] LayerMask m_trajectoryTracingCollisionMask;
    [SerializeField] SoundData m_fireSound;
    [SerializeField] SoundData m_hitSound;
    [SerializeField] SoundData m_embedSound;
    protected bool CompletedTrajectory;
    bool m_isPreview;
    Collider2D m_arrowCollider;
    Collider2D m_ignoredCollider;
    bool m_isIgnoringCollision;
    readonly List<Vector2> m_trajectoryPoints = new();
    [SerializeField] VFXData m_impactVFX;
    [field: FormerlySerializedAs("<Name>k__BackingField")] [field:SerializeField] public string NameForUI { get; protected set; }
    [field:SerializeField] public Sprite SpriteForUI { get; protected set; }

    void Awake()
    {
        RB = GetComponent<Rigidbody2D>();
        m_arrowCollider = GetComponent<Collider2D>();
    }

    public void SetPreview(bool preview, Collider2D playerCollider = null)
    {
        m_isPreview = preview;

        if (preview)
        {
            RB.linearVelocity = Vector2.zero;
            RB.angularVelocity = 0f;
            RB.bodyType = RigidbodyType2D.Kinematic;

            // Disable the preview arrow's collider so it doesn't interfere with trajectory raycasts
            if (m_arrowCollider != null)
            {
                m_arrowCollider.enabled = false;
            }

            // Store player collider reference
            if (playerCollider != null)
            {
                m_ignoredCollider = playerCollider;
            }
        }
        else
        {
            RB.bodyType = RigidbodyType2D.Dynamic;
            
            // Re-enable collider when firing
            if (m_arrowCollider != null)
            {
                m_arrowCollider.enabled = true;
            }
        }
    }

    public virtual void Fire(Vector2 direction, float powerPercentage, Collider2D playerCollider = null)
    {
        CompletedTrajectory = false;
        StuckInWall = false;
        if (m_arrowCollider != null)
        {
            m_arrowCollider.enabled = false;
        }
    
        RB.AddForce(direction.normalized * m_fireForce * powerPercentage, ForceMode2D.Impulse);
        if (playerCollider == null) return;
        m_ignoredCollider = playerCollider;
        m_isIgnoringCollision = false;
        SoundManager.Instance.CreateSound().WithSoundData(m_fireSound).WithRandomPitch().WithPosition(transform.position).Play();
    }

    void FixedUpdate()
    {
        if (CompletedTrajectory || m_isPreview) return;

        if (RB.linearVelocity.sqrMagnitude > 0.001f)
        {
            RB.rotation = Mathf.Atan2(RB.linearVelocity.y, RB.linearVelocity.x) * Mathf.Rad2Deg;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (m_arrowCollider != null && !m_arrowCollider.enabled)
        {
            m_arrowCollider.enabled = true;
            if (m_ignoredCollider != null)
            {
                Physics2D.IgnoreCollision(m_arrowCollider, m_ignoredCollider, true);
                m_isIgnoringCollision = true;
            }
        }
    
        if (!m_isIgnoringCollision || other != m_ignoredCollider) return;
        Physics2D.IgnoreCollision(m_arrowCollider, m_ignoredCollider, false);
        m_isIgnoringCollision = false;
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if(CompletedTrajectory){return;}
        OnImpact(collision);
    }

    protected virtual void OnImpact(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Arrow Surface") || LayerMask.LayerToName(collision.gameObject.layer) == "Arrow" && collision.gameObject.GetComponent<Arrow>().StuckInWall)
        {
            OnEmbed(collision);
        }
        else
        {
            OnHit(collision);
        }
    }

    protected virtual void OnEmbed(Collision2D collision)
    {
        RB.linearVelocity = Vector2.zero;
        RB.angularVelocity = 0f;
        RB.bodyType = RigidbodyType2D.Static;
        SoundManager.Instance.CreateSound().WithSoundData(m_embedSound).WithRandomPitch().WithPosition(transform.position).Play();
        StuckInWall = true;
        CompletedTrajectory = true;
    }
    protected virtual void OnHit(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent(out IDamageable damageable))
        {
            damageable.Damage(100);
        }
        SoundManager.Instance.CreateSound().WithSoundData(m_hitSound).WithRandomPitch().WithPosition(transform.position).Play();
        CompletedTrajectory = true;
    }

    void OnDisable()
    {
        if (m_isIgnoringCollision && m_ignoredCollider != null && m_arrowCollider != null)
        {
            Physics2D.IgnoreCollision(m_arrowCollider, m_ignoredCollider, false);
            m_isIgnoringCollision = false;
        }

        // Re-enable collider when disabled
        if (m_arrowCollider != null)
        {
            m_arrowCollider.enabled = true;
        }
    }

    public List<Vector2> CalculateTrajectory(Transform startTransform, float powerPercentage = 1f)
    {
        m_trajectoryPoints.Clear();

        Vector2 startPos = startTransform.position;
        Vector2 startVelocity = startTransform.right.normalized * (m_fireForce * powerPercentage);
        float gravityScale = RB?.gravityScale * RB?.mass ?? 1f;
        float gravity = Mathf.Abs(Physics2D.gravity.y) * gravityScale;
        

        Vector2 previousPoint = startPos;

        for (int i = 0; i < m_trajectoryPointCount; i++)
        {
            float time = i * m_trajectoryPointTime;
            
            // Calculate position using kinematic equations
            Vector2 point = startPos + startVelocity * time;
            point.y -= 0.5f * gravity * time * time;

            // Only do raycasts after the first point
            if (i > 0)
            {
                Vector2 delta = point - previousPoint;
                float distance = delta.magnitude;

                if (distance > 0.001f)
                {
                    RaycastHit2D hit = Physics2D.Raycast(
                        previousPoint, 
                        delta.normalized, 
                        distance, 
                        m_trajectoryTracingCollisionMask
                    );

                    if (hit.collider is not null)
                    {
                        // Check if we hit the player (ignore it during preview)
                        if (m_ignoredCollider is not null && hit.collider == m_ignoredCollider)
                        {
                            // Continue trajectory through player
                            m_trajectoryPoints.Add(point);
                            previousPoint = point;
                            continue;
                        }

                        // Hit something else - end trajectory here
                        m_trajectoryPoints.Add(hit.point);
                        break;
                    }
                }
            }

            m_trajectoryPoints.Add(point);
            previousPoint = point;
        }

        return m_trajectoryPoints;
    }
}

