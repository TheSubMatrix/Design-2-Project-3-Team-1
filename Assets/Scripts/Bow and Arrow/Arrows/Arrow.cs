using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] float m_launchGracePeriod = 0.15f; 
    float m_launchTime;
    protected bool CompletedTrajectory;
    bool m_isPreview;
    Collider2D m_arrowCollider;
    Collider2D m_ignoredCollider;
    bool m_isIgnoringCollision;
    readonly List<Vector4> m_trajectoryPoints = new();
    readonly List<float> m_cumulativeDistances = new();
    [SerializeField] VFXData m_impactVFX;
    Vector2 m_launchDirection;
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
        m_launchDirection = direction;
        m_launchTime = Time.time; 

        CompletedTrajectory = false;
        StuckInWall = false;
        RB.AddForce(direction.normalized * m_fireForce * powerPercentage, ForceMode2D.Impulse);
        
        if (playerCollider == null) return;
        m_ignoredCollider = playerCollider;
        Physics2D.IgnoreCollision(m_arrowCollider, m_ignoredCollider, true);
        m_isIgnoringCollision = true;
        SoundManager.Instance.CreateSound().WithSoundData(m_fireSound).WithRandomPitch().WithPosition(transform.position).Play();
    }

    protected virtual void FixedUpdate()
    {
        if (CompletedTrajectory || m_isPreview) return;

        if (RB.linearVelocity.sqrMagnitude > 0.001f)
        {
            RB.rotation = Mathf.Atan2(RB.linearVelocity.y, RB.linearVelocity.x) * Mathf.Rad2Deg;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!m_isIgnoringCollision || other != m_ignoredCollider) return;
        Physics2D.IgnoreCollision(m_arrowCollider, m_ignoredCollider, false);
        m_isIgnoringCollision = false;
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if(CompletedTrajectory || !IsValidCollision(collision)){return;}
        OnImpact(collision);
    }

    protected bool IsValidCollision(Collision2D collision)
    {
        if (collision.contactCount <= 0) return false;
        Vector2 contactNormal = collision.contacts.Aggregate(Vector2.zero, (current, contact) => current + contact.normal).normalized;
        if (!(Time.time < m_launchTime + m_launchGracePeriod)) return true;
        return !(Vector2.Dot(m_launchDirection, contactNormal) > 0);
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
    
    public List<Vector4> CalculateTrajectory(Transform startTransform, float powerPercentage = 1f)
    {
        m_trajectoryPoints.Clear();
        m_cumulativeDistances.Clear();

        Vector2 startPos = startTransform.position;
        Vector2 startVelocity = startTransform.right.normalized * (m_fireForce * powerPercentage);
        float gravityScale = RB?.gravityScale * RB?.mass ?? 1f;
        float gravity = Mathf.Abs(Physics2D.gravity.y) * gravityScale;
        Vector2 previousPoint = startPos;
        float totalDistance = 0f;
        m_trajectoryPoints.Add(new Vector4(startPos.x, startPos.y, 0f, 0f));
        m_cumulativeDistances.Add(0f);

        for (int i = 1; i < m_trajectoryPointCount; i++)
        {
            float time = i * m_trajectoryPointTime;

            Vector2 point = startPos + startVelocity * time;
            point.y -= 0.5f * gravity * time * time;

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
                    if (m_ignoredCollider is not null && hit.collider == m_ignoredCollider)
                    {
                        totalDistance += distance;
                        m_trajectoryPoints.Add(new Vector4(point.x, point.y, 0f, 0f));
                        m_cumulativeDistances.Add(totalDistance);
                        previousPoint = point;
                        continue;
                    }

                    float hitDistance = Vector2.Distance(previousPoint, hit.point);
                    totalDistance += hitDistance;
                    m_trajectoryPoints.Add(new Vector4(hit.point.x, hit.point.y, 0f, 0f));
                    m_cumulativeDistances.Add(totalDistance);
                    break;
                }
                
                totalDistance += distance;
            }

            m_trajectoryPoints.Add(new Vector4(point.x, point.y, 0f, 0f));
            m_cumulativeDistances.Add(totalDistance);
            previousPoint = point;
        }

        const float desiredDotSpacing = 0.5f;
        
        for (int i = 0; i < m_trajectoryPoints.Count; i++)
        {
            float uvCoordinate = m_cumulativeDistances[i] / desiredDotSpacing;
            float normalizedDistance = totalDistance > 0 ? m_cumulativeDistances[i] / totalDistance : 0f;
            
            
            m_trajectoryPoints[i] = new Vector4(
                m_trajectoryPoints[i].x,
                m_trajectoryPoints[i].y,
                uvCoordinate,
                normalizedDistance
            );
        }

        return m_trajectoryPoints;
    }
}
