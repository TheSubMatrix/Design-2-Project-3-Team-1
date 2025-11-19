using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;

public class ModelRotator : MonoBehaviour
{
    [System.Serializable]
    public class RotationAxis
    {
        [Header("Rotation Settings")]
        [Tooltip("The Axis around which to rotate")]
        public Vector3 Axis = Vector3.up;

        [Tooltip("Rotation Speed in degrees per second")]
        public float Speed = 45f;

        [Tooltip("Whether to rotate in world space or local space")]
        public Space RotationSpace = Space.Self;

        [Tooltip("Whether to consider the offset from the Axis vector")]
        public bool UseOffset;

        [Tooltip("Point on the Axis to rotate around (only used if UseOffset is true)"), ShowIf(nameof(UseOffset))]
        public Vector3 OffsetPoint = Vector3.zero;
    
        [Tooltip("Whether the offset point is in world space or local space"), ShowIf(nameof(UseOffset))]
        public Space OffsetSpace = Space.Self;
    
        [Header("Debug Visualization")]
        [Tooltip("Enable debug visualization for this axis")]
        public bool ShowDebug = true;
        
        [Tooltip("Color for the debug axis line"), ShowIf(nameof(ShowDebug))]
        public Color DebugColor = Color.red;
    
        [Tooltip("Length of the debug axis line (in each direction from center)"), ShowIf(nameof(ShowDebug))]
        public float DebugAxisLength = 2f;

        bool DebugAndOffset() => ShowDebug && UseOffset;
        [Tooltip("Radius of the debug pivot sphere"), ShowIf(nameof(DebugAndOffset))]
        public float DebugSphereRadius = 0.1f;
    }
    
    [FormerlySerializedAs("rotationAxes")] [Header("Rotation Configuration")] [SerializeField]
    List<RotationAxis> m_rotationAxes = new();
    void Start()
    {
        foreach (RotationAxis axis in m_rotationAxes)
        {
            axis.Axis = axis.Axis.normalized;
        }
    }

    void Update()
    {
        // Apply all rotations
        foreach (RotationAxis rotAxis in m_rotationAxes)
        {
            ApplyRotation(rotAxis);
        }
    }

    void ApplyRotation(RotationAxis rotAxis)
    {
        float angle = rotAxis.Speed * Time.deltaTime;
        
        if (rotAxis.UseOffset)
        {
            RotateAroundPoint(rotAxis, angle);
        }
        else
        {
            transform.Rotate(rotAxis.Axis, angle, rotAxis.RotationSpace == Space.World ? Space.World : Space.Self);
        }
    }

    void RotateAroundPoint(RotationAxis rotAxis, float angle)
    {
        Vector3 axisWorld = rotAxis.RotationSpace == Space.World 
            ? rotAxis.Axis 
            : transform.TransformDirection(rotAxis.Axis);
        
        Vector3 pivotPoint = rotAxis.OffsetSpace == Space.World 
            ? rotAxis.OffsetPoint 
            : transform.TransformPoint(rotAxis.OffsetPoint);
    
        // Create rotation quaternion
        Quaternion rotation = Quaternion.AngleAxis(angle, axisWorld);
    
        // Calculate the new position
        Vector3 offset = transform.position - pivotPoint;
        Vector3 newOffset = rotation * offset;
        transform.position = pivotPoint + newOffset;
    
        // Apply rotation to the object itself
        transform.rotation = rotation * transform.rotation;
    }

    // Public methods for runtime control
    public void AddRotationAxis(RotationAxis newAxis)
    {
        newAxis.Axis = newAxis.Axis.normalized;
        m_rotationAxes.Add(newAxis);
    }

    public void RemoveRotationAxis(int index)
    {
        if (index >= 0 && index < m_rotationAxes.Count)
        {
            m_rotationAxes.RemoveAt(index);
        }
    }

    public void ClearAllAxes()
    {
        m_rotationAxes.Clear();
    }


    // Visualize rotation axes in the editor
    void OnDrawGizmos()
    {
        if (m_rotationAxes == null || m_rotationAxes.Count == 0) return;

        foreach (RotationAxis rotAxis in m_rotationAxes.Where(rotAxis => rotAxis.ShowDebug))
        {
            Gizmos.color = rotAxis.DebugColor;
    
            Vector3 startPos = rotAxis.UseOffset 
                ? (rotAxis.OffsetSpace == Space.World ? rotAxis.OffsetPoint : transform.TransformPoint(rotAxis.OffsetPoint))
                : transform.position;
    
            Vector3 axisDir = rotAxis.RotationSpace == Space.World 
                ? rotAxis.Axis 
                : transform.TransformDirection(rotAxis.Axis);
    
            // Draw Axis line with a configurable length
            Gizmos.DrawLine(startPos - axisDir * rotAxis.DebugAxisLength, startPos + axisDir * rotAxis.DebugAxisLength);
    
            // Draw sphere at the pivot point with a configurable radius
            if (rotAxis.UseOffset)
            {
                Gizmos.DrawWireSphere(startPos, rotAxis.DebugSphereRadius);
            }
        }
    }
}