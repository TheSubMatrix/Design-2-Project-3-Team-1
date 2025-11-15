using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChainArrow : Arrow
{
    [Header("Chain Properties")]
    [SerializeField] float m_linkLength = 0.5f;
    [SerializeField] float m_linkWidth = 0.2f;
    [SerializeField] float m_linkMass = 1f;
    [SerializeField] LayerMask m_chainInterruptionMask;
    
    [Header("Physics")]
    [SerializeField] float m_damping = 0.5f;
    [SerializeField] float m_angularDamping = 0.5f;
    [SerializeField] PhysicsMaterial2D m_physicsMaterial;
    
    [Header("Joint Properties")]
    [SerializeField][Range(0f, 100f)] float m_jointBreakForce;
    [SerializeField][Range(0f, 100f)] float m_jointBreakTorque;
    
    [Header("Prefab & Pooling")]
    [SerializeField] GameObject m_linkPrefab;
    [SerializeField] int m_poolInitialSize = 20;
    
    // Static pool shared across all ChainArrows for efficiency
    static Queue<GameObject> s_linkPool;
    static Transform s_poolParent;
    static bool s_poolInitialized;
    
    // Instance-specific active links
    readonly List<GameObject> m_activeLinks = new();
    GameObject m_firstLink;
    GameObject m_lastLink;
    
    // Connection tracking
    bool m_triedCreatingChain;
    static readonly List<ChainArrow> s_validConnectionPoints = new();
    
    // Cached components
    struct LinkComponents
    {
        public Rigidbody2D RB;
        public BoxCollider2D Collider;
        public Transform Transform;
    }
    
    void Start()
    {
        InitializePoolIfNeeded();
    }
    
    void InitializePoolIfNeeded()
    {
        if (s_poolInitialized) return;
        
        // Create a shared pool parent in DontDestroyOnLoad
        GameObject poolObj = new("ChainLinkPool_Shared");
        DontDestroyOnLoad(poolObj);
        s_poolParent = poolObj.transform;
        
        s_linkPool = new Queue<GameObject>();
        
        // Pre-instantiate pool
        for (int i = 0; i < m_poolInitialSize; i++)
        {
            GameObject link = CreateNewLink();
            link.SetActive(false);
            s_linkPool.Enqueue(link);
        }
        
        s_poolInitialized = true;
    }
    
    GameObject CreateNewLink()
    {
        GameObject link;
        
        if (m_linkPrefab != null)
        {
            link = Instantiate(m_linkPrefab, s_poolParent);
        }
        else
        {
            link = new GameObject("ChainLink");
            link.transform.SetParent(s_poolParent);
            
            Rigidbody2D rb = link.AddComponent<Rigidbody2D>();
            rb.mass = m_linkMass;
            rb.linearDamping = m_damping;
            rb.angularDamping = m_angularDamping;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            
            BoxCollider2D col = link.AddComponent<BoxCollider2D>();
            col.size = new Vector2(m_linkLength, m_linkWidth);
            if (m_physicsMaterial) col.sharedMaterial = m_physicsMaterial;
            
            // Simple visual
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (visual.TryGetComponent<Collider>(out var collider3D))
            {
                if (Application.isPlaying) Destroy(collider3D);
                else DestroyImmediate(collider3D);
            }
            visual.transform.SetParent(link.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(m_linkLength, m_linkWidth, m_linkWidth) * 0.9f;
        }
        
        return link;
    }
    
    GameObject GetLinkFromPool()
    {
        GameObject link = s_linkPool.Count > 0 ? s_linkPool.Dequeue() : CreateNewLink();
        link.SetActive(true);
        m_activeLinks.Add(link);
        return link;
    }
    
    void ReturnLinkToPool(GameObject link)
    {
        // Clean up joints
        HingeJoint2D[] joints = link.GetComponents<HingeJoint2D>();
        foreach (HingeJoint2D joint in joints)
        {
            if (Application.isPlaying) Destroy(joint);
            else DestroyImmediate(joint);
        }
        
        // Reset rigidbody
        if (link.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        
        link.SetActive(false);
        s_linkPool.Enqueue(link);
    }
    
    protected override void OnEmbed(Collision2D collision)
    {
        base.OnEmbed(collision);
        Debug.Log("OnEmbed called - attempting to create chain");
        TryCreateChain();
    }
    void TryCreateChain()
    {
        if (m_triedCreatingChain)
        {
            Debug.Log("Already tried creating chain, skipping");
            return;
        }
        m_triedCreatingChain = true;
        
        Debug.Log($"TryCreateChain called. Valid connection points count: {s_validConnectionPoints.Count}");
        
        // Find a valid connection point (no obstacles between)
        ChainArrow targetArrow = null;
        
        foreach (ChainArrow arrow in s_validConnectionPoints)
        {
            if (arrow == null || arrow == this) continue;
            
            Vector2 startPos = transform.position;
            Vector2 endPos = arrow.transform.position;
            float distance = Vector2.Distance(startPos, endPos);
            
            Debug.Log($"Checking arrow at distance {distance}. Mask value: {m_chainInterruptionMask.value}");
            
            // Check if there's a clear path
            RaycastHit2D hit = Physics2D.Linecast(startPos, endPos, m_chainInterruptionMask);
            
            if (hit.collider != null)
            {
                Debug.Log($"Linecast blocked by: {hit.collider.gameObject.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            }
            else
            {
                Debug.Log($"Clear path found to {arrow.gameObject.name}!");
                targetArrow = arrow;
                break;
            }
        }
        
        if (targetArrow != null)
        {
            Debug.Log($"Creating chain to {targetArrow.gameObject.name}");
            s_validConnectionPoints.Remove(targetArrow);
            GenerateChain(transform.position, targetArrow.transform.position, RB, targetArrow.RB);
        }
        else
        {
            s_validConnectionPoints.Add(this);
            Debug.Log($"Added to connection list. Total arrows waiting: {s_validConnectionPoints.Count}");
        }
    }
    
    bool GenerateChain(Vector2 startPoint, Vector2 endPoint, Rigidbody2D startAnchor, Rigidbody2D endAnchor)
    {
        ClearChain();
        
        float distance = Vector2.Distance(startPoint, endPoint);
        int linkCount = Mathf.Max(2, Mathf.CeilToInt(distance / m_linkLength));
        Vector2 direction = (endPoint - startPoint).normalized;
        float actualLinkLength = distance / linkCount;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        
        LinkComponents prevComponents = default;
        bool hasPrevious = false;
        
        Debug.Log($"Generating chain with {linkCount + 1} links");
        
        // Generate all links
        for (int i = 0; i <= linkCount; i++)
        {
            float t = (float)i / linkCount;
            Vector2 position = Vector2.Lerp(startPoint, endPoint, t);
            
            GameObject link = GetLinkFromPool();
            Transform linkTransform = link.transform;
            linkTransform.position = new Vector3(position.x, position.y, 0);
            linkTransform.rotation = rotation;
            
            // Cache components
            LinkComponents components;
            components.Transform = linkTransform;
            components.RB = link.GetComponent<Rigidbody2D>();
            components.Collider = link.GetComponent<BoxCollider2D>();
            
            // Update collider size if needed
            if (!Mathf.Approximately(components.Collider.size.x, actualLinkLength))
            {
                components.Collider.size = new Vector2(actualLinkLength, m_linkWidth);
            }
            
            if (i == 0) m_firstLink = link;
            if (i == linkCount) m_lastLink = link;
            
            // Connect to the previous link
            if (hasPrevious)
            {
                ConnectLinks(prevComponents, components, actualLinkLength);
            }
            
            prevComponents = components;
            hasPrevious = true;
        }
        
        // Anchor to arrows
        if (m_firstLink != null && startAnchor != null)
        {
            AnchorLink(m_firstLink, startAnchor, true, actualLinkLength);
        }
        
        if (m_lastLink != null && endAnchor != null)
        {
            AnchorLink(m_lastLink, endAnchor, false, actualLinkLength);
        }
        
        Debug.Log("Chain generation complete!");
        return true;
    }
    
    void ConnectLinks(LinkComponents linkA, LinkComponents linkB, float length)
    {
        HingeJoint2D joint = linkA.Transform.gameObject.AddComponent<HingeJoint2D>();
        joint.connectedBody = linkB.RB;
        joint.autoConfigureConnectedAnchor = false;
        
        Vector2 anchorOffset = new(length * 0.5f, 0);
        joint.anchor = anchorOffset;
        joint.connectedAnchor = -anchorOffset;
        
        joint.useLimits = true;
        joint.limits = new JointAngleLimits2D { min = -45f, max = 45f };
        
        if (m_jointBreakForce > 0) joint.breakForce = m_jointBreakForce;
        if (m_jointBreakTorque > 0) joint.breakTorque = m_jointBreakTorque;
    }
    
    void AnchorLink(GameObject link, Rigidbody2D anchor, bool isStart, float linkLength)
    {
        HingeJoint2D joint = link.AddComponent<HingeJoint2D>();
        joint.connectedBody = anchor;
        joint.autoConfigureConnectedAnchor = false;
        
        Vector2 anchorOffset = new(isStart ? -linkLength * 0.5f : linkLength * 0.5f, 0);
        joint.anchor = anchorOffset;
        
        Vector2 worldPos = link.transform.TransformPoint(anchorOffset);
        joint.connectedAnchor = anchor.transform.InverseTransformPoint(worldPos);
        
        joint.useLimits = true;
        joint.limits = new JointAngleLimits2D { min = -45f, max = 45f };
        
        if (m_jointBreakForce > 0) joint.breakForce = m_jointBreakForce;
        if (m_jointBreakTorque > 0) joint.breakTorque = m_jointBreakTorque;
    }

    void ClearChain()
    {
        foreach (GameObject link in m_activeLinks.Where(link => link != null))
        {
            ReturnLinkToPool(link);
        }
        m_activeLinks.Clear();
        m_firstLink = null;
        m_lastLink = null;
    }
    
    void OnDestroy()
    {
        ClearChain();
        s_validConnectionPoints.Remove(this);
    }
}