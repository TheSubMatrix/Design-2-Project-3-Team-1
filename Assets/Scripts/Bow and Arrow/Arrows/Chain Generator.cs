using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ChainGenerator : MonoBehaviour
{
    // Chain properties
    [FormerlySerializedAs("linkLength")]
    [Header("Chain Properties")]
    [SerializeField] float m_linkLength = 0.5f;
    [FormerlySerializedAs("linkWidth")] [SerializeField] float m_linkWidth = 0.2f;
    [FormerlySerializedAs("linkMass")] [SerializeField] float m_linkMass = 1f;
    
    // Physics properties
    [FormerlySerializedAs("damping")]
    [Header("Physics Properties")]
    [SerializeField] float m_damping = 0.5f;
    [FormerlySerializedAs("angularDamping")] [SerializeField] float m_angularDamping = 0.5f;
    [FormerlySerializedAs("physicsMaterial")] [SerializeField]  PhysicsMaterial2D m_physicsMaterial;
    
    // Joint properties (HingeJoint2D only)
    [FormerlySerializedAs("jointBreakForce")]
    [Header("Joint Properties")]
    [Tooltip("If greater than 0, chain links can break.")]
    [SerializeField][Range(0f, 100f)] float m_jointBreakForce;
    [FormerlySerializedAs("jointBreakTorque")] [SerializeField][Range(0f, 100f)]  float m_jointBreakTorque;
    
    // Collision/path check
    [FormerlySerializedAs("collisionCheckLayer")]
    [Header("Collision Check")]
    [SerializeField] LayerMask m_collisionCheckLayer = -1;
    [FormerlySerializedAs("raycastRadius")] [SerializeField] float m_raycastRadius = 0.1f;
    
    // Visuals
    [FormerlySerializedAs("linkModelPrefab")]
    [Header("Visual (3D Model)")]
    [SerializeField] GameObject m_linkModelPrefab;
    [FormerlySerializedAs("modelScale")] [SerializeField] Vector3 m_modelScale = Vector3.one;
    
    // Anchoring
    [FormerlySerializedAs("anchorStart")]
    [Header("Anchor Options")]
    [SerializeField] bool m_anchorStart = true;
    [FormerlySerializedAs("anchorEnd")] [SerializeField] bool m_anchorEnd;
    
    // Editor testing
    [FormerlySerializedAs("testStartPoint")]
    [Header("Editor Testing")]
    [SerializeField] Transform m_testStartPoint;
    [FormerlySerializedAs("testEndPoint")] [SerializeField] Transform m_testEndPoint;
    [FormerlySerializedAs("showDebugPath")] [SerializeField] bool m_showDebugPath = true;
    
    GameObject m_firstLink;
    GameObject m_lastLink;
    
    /// <summary>Generates a chain between two points if the path is clear.</summary>
    public bool GenerateChain(Vector2 startPoint, Vector2 endPoint, Rigidbody2D startAnchor = null, Rigidbody2D endAnchor = null)
    {
        ClearChain();

        if (!IsPathClear(startPoint, endPoint))
        {
            Debug.LogWarning("Cannot create chain - path is blocked!");
            return false;
        }
        
        float distance = Vector2.Distance(startPoint, endPoint);
        int linkCount = Mathf.Max(2, Mathf.CeilToInt(distance / m_linkLength));
        Vector2 direction = (endPoint - startPoint).normalized;
        float actualLinkLength = distance / linkCount;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        GameObject previousLink = null;
        
        for (int i = 0; i <= linkCount; i++)
        {
            float t = (float)i / linkCount;
            Vector2 position = Vector2.Lerp(startPoint, endPoint, t);
            
            GameObject link = CreateChainLink(position, actualLinkLength, angle);
            
            if (i == 0) m_firstLink = link;
            if (i == linkCount) m_lastLink = link;
            
            if (previousLink != null)
            {
                ConnectLinks(previousLink, link, actualLinkLength);
            }
            
            previousLink = link;
        }
        
        // Handle Anchoring
        if (startAnchor && m_firstLink)
        {
            AnchorLinkToObject(m_firstLink, startAnchor, true, actualLinkLength);
        }
        else if (m_anchorStart && m_firstLink)
        {
            m_firstLink.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        }
        
        if (endAnchor && m_lastLink)
        {
            AnchorLinkToObject(m_lastLink, endAnchor, false, actualLinkLength);
        }
        else if (m_anchorEnd && m_lastLink)
        {
            m_lastLink.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        }
        
        Debug.Log($"Chain created successfully with {linkCount + 1} links");
        return true;
    }
    
    /// <summary>Checks if the path between two points is clear.</summary>
    private bool IsPathClear(Vector2 startPoint, Vector2 endPoint)
    {
        Vector2 direction = endPoint - startPoint;
        float distance = direction.magnitude;
        
        RaycastHit2D hit = Physics2D.CircleCast(
            startPoint, m_raycastRadius, direction.normalized, distance, m_collisionCheckLayer
        );
        
        if (m_showDebugPath)
        {
            Debug.DrawLine(startPoint, endPoint, !hit.collider ? Color.green : Color.red, 2f);
        }
        
        return !hit.collider;
    }
    
    /// <summary>Creates a single chain link.</summary>
    private GameObject CreateChainLink(Vector2 position, float length, float rotationAngle)
    {
        GameObject link = new("ChainLink")
        {
            transform =
            {
                position = new Vector3(position.x, position.y, 0),
                rotation = Quaternion.Euler(0, 0, rotationAngle)
            }
        };
        link.transform.SetParent(transform);
        
        Rigidbody2D rb = link.AddComponent<Rigidbody2D>();
        rb.mass = m_linkMass;
        rb.linearDamping = m_damping;
        rb.angularDamping = m_angularDamping;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        BoxCollider2D colliderForLink = link.AddComponent<BoxCollider2D>();
        colliderForLink.size = new Vector2(length, m_linkWidth);
        if (m_physicsMaterial)
        {
            colliderForLink.sharedMaterial = m_physicsMaterial;
        }
        
        CreateLinkVisual(link, length);
        
        return link;
    }
    
    /// <summary>Creates visual representation for a link.</summary>
    private void CreateLinkVisual(GameObject link, float length)
    {
        if (!m_linkModelPrefab)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            // Remove 3D collider
            Collider colliderToRemove = visual.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(colliderToRemove);
            else DestroyImmediate(colliderToRemove);
            
            visual.transform.SetParent(link.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(length, m_linkWidth, m_linkWidth) * 0.9f;
        }
        else
        {
            GameObject visual = Instantiate(m_linkModelPrefab, link.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = m_modelScale;
            
            // Remove 3D colliders from model
            Collider[] colliders = visual.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
        }
    }
    
    /// <summary>Connects two links with a HingeJoint2D.</summary>
    private void ConnectLinks(GameObject linkA, GameObject linkB, float length)
    {
        HingeJoint2D joint = linkA.AddComponent<HingeJoint2D>();
        joint.connectedBody = linkB.GetComponent<Rigidbody2D>();
        joint.autoConfigureConnectedAnchor = false;
        
        // Anchors at the edges of each link
        Vector2 anchorOffset = new Vector2(length / 2, 0);
        joint.anchor = anchorOffset;
        joint.connectedAnchor = -anchorOffset;
        
        // Angle limits
        joint.useLimits = true;
        JointAngleLimits2D limits = new JointAngleLimits2D { min = -45f, max = 45f };
        joint.limits = limits;
        
        // Break forces
        if (m_jointBreakForce > 0) joint.breakForce = m_jointBreakForce;
        if (m_jointBreakTorque > 0) joint.breakTorque = m_jointBreakTorque;
    }
    
    /// <summary>Anchors a chain link to a Rigidbody2D object.</summary>
    private void AnchorLinkToObject(GameObject link, Rigidbody2D anchorObject, bool isStart, float linkLength)
    {
        HingeJoint2D anchorJoint = link.AddComponent<HingeJoint2D>();
        anchorJoint.connectedBody = anchorObject;
        anchorJoint.autoConfigureConnectedAnchor = false;
        
        // Anchor at the appropriate end of the link
        Vector2 anchorOffset = new(isStart ? -linkLength / 2 : linkLength / 2, 0);
        anchorJoint.anchor = anchorOffset;
        
        // Calculate connected anchor in anchorObject's local space
        Vector2 worldAnchorPos = link.transform.TransformPoint(anchorOffset);
        anchorJoint.connectedAnchor = anchorObject.transform.InverseTransformPoint(worldAnchorPos);
        
        // Angle limits
        anchorJoint.useLimits = true;
        JointAngleLimits2D limits = new() { min = -45f, max = 45f };
        anchorJoint.limits = limits;
        
        // Break forces
        if (m_jointBreakForce > 0) anchorJoint.breakForce = m_jointBreakForce;
        if (m_jointBreakTorque > 0) anchorJoint.breakTorque = m_jointBreakTorque;
        Debug.Log($"Anchored {(isStart ? "start" : "end")} of chain to {anchorObject.gameObject.name}");
    }
    
    /// <summary>Helper to generate a chain from transform positions.</summary>
    public bool GenerateChain(Transform start, Transform end)
    {
        Rigidbody2D startRb = m_anchorStart ? start.GetComponent<Rigidbody2D>() : null;
        Rigidbody2D endRb = m_anchorEnd ? end.GetComponent<Rigidbody2D>() : null;
        
        return GenerateChain(start.position, end.position, startRb, endRb);
    }
    
    /// <summary>Anchors the existing chain to objects (using serialized linkLength).</summary>
    public void AnchorChainToObjects(Rigidbody2D startAnchor, Rigidbody2D endAnchor)
    {
        if (m_firstLink != null && startAnchor != null)
        {
            Rigidbody2D rb = m_firstLink.GetComponent<Rigidbody2D>();
            if (rb.bodyType == RigidbodyType2D.Kinematic) rb.bodyType = RigidbodyType2D.Dynamic;
            AnchorLinkToObject(m_firstLink, startAnchor, true, m_linkLength);
        }

        if (m_lastLink == null || endAnchor == null) return;
        {
            Rigidbody2D rb = m_lastLink.GetComponent<Rigidbody2D>();
            if (rb.bodyType == RigidbodyType2D.Kinematic) rb.bodyType = RigidbodyType2D.Dynamic;
            AnchorLinkToObject(m_lastLink, endAnchor, false, m_linkLength);
        }
    }
    
    /// <summary>Gets the first link.</summary>
    public GameObject GetFirstLink() { return m_firstLink; }
    
    /// <summary>Gets the last link.</summary>
    public GameObject GetLastLink() { return m_lastLink; }
    
    /// <summary>Clears all chain links created by this generator.</summary>
    public void ClearChain()
    {
        m_firstLink = null;
        m_lastLink = null;
        
        Transform[] children = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            children[i] = transform.GetChild(i);
        }
        
        foreach (Transform child in children)
        {
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
    }
    
    /// <summary>Editor test method.</summary>
    public void TestGenerateChain()
    {
        if (!m_testStartPoint || !m_testEndPoint)
        {
            Debug.LogError("Test start and end points must be assigned!");
            return;
        }
        
        // ClearChain() is called inside GenerateChain()
        GenerateChain(m_testStartPoint.position, m_testEndPoint.position);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ChainGenerator))]
public class ChainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        ChainGenerator generator = (ChainGenerator)target;
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Editor Testing", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Generate Test Chain", GUILayout.Height(30)))
        {
            generator.TestGenerateChain();
        }
        
        if (GUILayout.Button("Clear Chain", GUILayout.Height(30)))
        {
            generator.ClearChain();
        }
    }
}
#endif