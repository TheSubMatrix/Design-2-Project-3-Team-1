using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(Rigidbody2D))]
public class Arrow : MonoBehaviour
{
    bool m_completedTrajectory = false;
    public bool StuckInWall { get; protected set; }
    [SerializeField] float m_fireForce = 10;
    [SerializeField] uint m_trajectoryPointCount = 20;
    [SerializeField] float m_trajectoryPointTime = 0.1f;
    [SerializeField] LayerMask m_collisionMask;
    public Rigidbody2D RB { get; protected set; }
    readonly List<Vector2> m_trajectoryPoints = new();
    
    Collider2D m_arrowCollider;
    Collider2D m_ignoredCollider;
    bool m_isIgnoringCollision = false;

    void Awake()
    {
        RB = GetComponent<Rigidbody2D>();
        m_arrowCollider = GetComponent<Collider2D>();
    }
    
    public virtual void Fire(Vector2 direction, Collider2D playerCollider = null)
    {
        m_completedTrajectory = false;
        RB.AddForce(direction.normalized * m_fireForce, ForceMode2D.Impulse);
        StuckInWall = false;

        if (playerCollider == null) return;
        m_ignoredCollider = playerCollider;
        Physics2D.IgnoreCollision(m_arrowCollider, playerCollider, true);
        m_isIgnoringCollision = true;
    }

    void FixedUpdate()
    {
        if(m_completedTrajectory) return;
        RB.rotation = Mathf.Atan2(RB.linearVelocity.y, RB.linearVelocity.x) * Mathf.Rad2Deg;
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log("Exit");
        if (!m_isIgnoringCollision || other != m_ignoredCollider) return;
        Physics2D.IgnoreCollision(m_arrowCollider, m_ignoredCollider, false);
        m_isIgnoringCollision = false;
    }
    
    protected virtual void OnImpact(Collision2D collision)
    {
        if (collision.gameObject.layer.Equals(LayerMask.NameToLayer("Arrow Surface")))
        {
            StuckInWall = true;
            RB.bodyType = RigidbodyType2D.Static;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        m_completedTrajectory = true;
        OnImpact(collision);
    }
    
    void OnDisable()
    {
        if (!m_isIgnoringCollision || m_ignoredCollider == null || m_arrowCollider == null) return;
        Physics2D.IgnoreCollision(m_arrowCollider, m_ignoredCollider, false);
        m_isIgnoringCollision = false;
    }
    
    public List<Vector2> CalculateTrajectory(Transform startTransform)
    {
        Vector2 startVelocity = startTransform.right.normalized * m_fireForce;
        Vector2 startPos = startTransform.position;
        
        m_trajectoryPoints.Clear();
        
        Vector2 previousPoint = startPos;
        
        for (float time = 0; time < m_trajectoryPointCount * m_trajectoryPointTime; time += m_trajectoryPointTime)
        {
            Vector2 point = startPos + startVelocity * time;
            point.y += 0.5f * Physics2D.gravity.y * time * time;
            if (time > 0)
            {
                Vector2 direction = point - previousPoint;
                float distance = direction.magnitude;
                
                RaycastHit2D hit = Physics2D.Raycast(previousPoint, direction.normalized, distance, m_collisionMask);
                
                if (hit.collider != null)
                {
                    m_trajectoryPoints.Add(hit.point);
                    break;
                }
            }
            
            m_trajectoryPoints.Add(point);
            previousPoint = point;
        }
        
        return m_trajectoryPoints;
    }
}