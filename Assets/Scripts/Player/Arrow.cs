using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Arrow : MonoBehaviour
{
    public Rigidbody2D RB { get; private set; }
    public bool StuckInWall { get; protected set; }

    [SerializeField] float m_fireForce = 10f;
    [SerializeField] uint m_trajectoryPointCount = 20;
    [SerializeField] float m_trajectoryPointTime = 0.1f;
    [SerializeField] LayerMask m_collisionMask;

    protected bool CompletedTrajectory;
    bool m_isPreview;
    Collider2D m_arrowCollider;
    Collider2D m_ignoredCollider;
    bool m_isIgnoringCollision;
    readonly List<Vector2> m_trajectoryPoints = new();

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
        RB.AddForce(direction.normalized * m_fireForce * powerPercentage, ForceMode2D.Impulse);
        
        if (playerCollider != null)
        {
            m_ignoredCollider = playerCollider;
            Physics2D.IgnoreCollision(m_arrowCollider, m_ignoredCollider, true);
            m_isIgnoringCollision = true;
        }
    }

    private void FixedUpdate()
    {
        if (CompletedTrajectory || m_isPreview) return;

        if (RB.linearVelocity.sqrMagnitude > 0.001f)
        {
            RB.rotation = Mathf.Atan2(RB.linearVelocity.y, RB.linearVelocity.x) * Mathf.Rad2Deg;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!m_isIgnoringCollision || other != m_ignoredCollider) return;

        Physics2D.IgnoreCollision(m_arrowCollider, m_ignoredCollider, false);
        m_isIgnoringCollision = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        OnImpact(collision);
    }

    protected virtual void OnImpact(Collision2D collision)
    {
        CompletedTrajectory = true;
        if (collision.gameObject.layer == LayerMask.NameToLayer("Arrow Surface"))
        {
            StuckInWall = true;
            RB.bodyType = RigidbodyType2D.Static;
        }
    }

    private void OnDisable()
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
                        m_collisionMask
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