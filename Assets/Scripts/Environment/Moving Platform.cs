#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] public Vector2 m_pointB = new(5f, 0f);
    [SerializeField] public Vector2 m_tangentA = new(1f, 0f);
    [SerializeField] public Vector2 m_tangentB = new(-1f, -0f);
    [SerializeField] float m_moveTime = 2f;
    public Vector2 Position => transform.position;

    Rigidbody2D m_rb;
    float m_progress;
    Vector2 m_startPointA;
    bool m_isMovingToB;

    void Awake()
    {
        m_rb = GetComponent<Rigidbody2D>();
        // Cache the starting position at runtime
        m_startPointA = transform.position;
        m_progress = 0f;
    }

    void FixedUpdate()
    {
        if (!m_rb || m_moveTime <= 0f) return;
        float step = Time.fixedDeltaTime / m_moveTime;
        m_progress = Mathf.MoveTowards(m_progress, m_isMovingToB ? 1f : 0f, step);
        Vector2 p0 = m_startPointA;
        Vector2 p1 = m_startPointA + m_tangentA;
        Vector2 p2 = m_pointB + m_tangentB;
        Vector2 p3 = m_pointB;

        // Apply a SmoothStep ease-in/ease-out to the linear progress
        float smoothedProgress = Mathf.SmoothStep(0f, 1f, m_progress);

        // Get the new position on the curve based on our smoothed progress
        Vector2 newPos = CalculateBezierPoint(smoothedProgress, p0, p1, p2, p3);

        // Move the platform using its Rigidbody2D
        m_rb.MovePosition(newPos);
    }

    /// <summary>
    /// Sets the target direction for the platform.
    /// </summary>
    /// <param name="moveToB">True to move towards Point B, False to move towards Point A (start).</param>
    public void SetMoveDirection(bool moveToB)
    {
        m_isMovingToB = moveToB;
    }

    /// <summary>
    /// Calculates a point on a cubic Bézier curve.
    /// </summary>
    /// <param name="t">The progress along the curve (0.0 to 1.0)</param>
    /// <param name="p0">Start point</param>
    /// <param name="p1">Start tangent control point</param>
    /// <param name="p2">End tangent control point</param>
    /// <param name="p3">End point</param>
    /// <returns>The position on the curve at time t</returns>
    static Vector2 CalculateBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector2 p = uuu * p0;
        p += 3f * uu * t * p1;
        p += 3f * u * tt * p2;
        p += ttt * p3;

        return p;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MovingPlatform)), CanEditMultipleObjects]
public class MovingPlatformEditor : Editor
{
    SerializedProperty m_pointBProp;
    SerializedProperty m_tangentAProp;
    SerializedProperty m_tangentBProp;
    SerializedProperty m_moveTimeProp; // Changed from m_speedProp

    void OnEnable()
    {
        m_pointBProp = serializedObject.FindProperty("m_pointB");
        m_tangentAProp = serializedObject.FindProperty("m_tangentA");
        m_tangentBProp = serializedObject.FindProperty("m_tangentB");
        m_moveTimeProp = serializedObject.FindProperty("m_moveTime"); // Fixed to find m_moveTime
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(m_pointBProp, new GUIContent("Point B"));
        EditorGUILayout.PropertyField(m_tangentAProp, new GUIContent("Tangent A"));
        EditorGUILayout.PropertyField(m_tangentBProp, new GUIContent("Tangent B"));
        EditorGUILayout.PropertyField(m_moveTimeProp, new GUIContent("Move Time")); // Added field for move time

        serializedObject.ApplyModifiedProperties();
    }

    void OnSceneGUI()
    {
        MovingPlatform platform = (MovingPlatform)target;
        
        // --- Point B Handle ---
        EditorGUI.BeginChangeCheck();
        Handles.color = Color.red;
        // Point B is absolute, so we just draw its handle
        Vector3 newPointB = Handles.PositionHandle(platform.m_pointB, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Move Point B");
            platform.m_pointB = newPointB;
            EditorUtility.SetDirty(target);
        }

        // --- Tangent A Handle ---
        // Tangent A is relative to the platform's transform position (Point A)
        Vector2 tangentPointA = platform.Position + platform.m_tangentA;
        EditorGUI.BeginChangeCheck();
        Handles.color = Color.cyan;
        Vector3 newTangentA = Handles.PositionHandle(tangentPointA, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Move Tangent A");
            platform.m_tangentA = (Vector2)newTangentA - platform.Position;
            EditorUtility.SetDirty(target);
        }

        // --- Tangent B Handle ---
        // Tangent B is relative to Point B
        Vector2 tangentPointB = platform.m_pointB + platform.m_tangentB;
        EditorGUI.BeginChangeCheck();
        Handles.color = Color.magenta;
        Vector3 newTangentB = Handles.PositionHandle(tangentPointB, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Move Tangent B");
            platform.m_tangentB = (Vector2)newTangentB - platform.m_pointB;
            EditorUtility.SetDirty(target);
        }

        // --- Draw Lines ---
        // Draw tangent lines
        Handles.color = Color.cyan;
        Handles.DrawDottedLine(platform.Position, tangentPointA, 3f);
        Handles.color = Color.magenta;
        Handles.DrawDottedLine(platform.m_pointB, tangentPointB, 3f);

        // Draw bezier curve
        Handles.DrawBezier(
            platform.Position,
            platform.m_pointB,
            tangentPointA,
            tangentPointB,
            Color.yellow,
            null,
            3f
        );
    }
}
#endif