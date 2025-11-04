using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class Bow : MonoBehaviour
{
    [Header("Arrow & Trajectory")]
    [SerializeField] List<ArrowPool> m_arrowPools;
    [SerializeField] Transform m_arrowSpawnPoint;
    [SerializeField] VisualEffect m_trajectoryEffect;
    [SerializeField] Collider2D m_playerCollider;
    [SerializeField, Range(1, 50)] uint m_trajectoryPointCount = 20;
    [SerializeField, Range(0.1f, 3f)] float m_maxPower = 1f;

    [Header("Input Actions")]
    [SerializeField] InputActionReference m_fireAction;
    [SerializeField] InputActionReference m_swapArrowAction;
    [SerializeField] InputActionReference m_aimAction;

    Camera m_mainCamera;
    GraphicsBuffer m_trajectoryBuffer;
    Vector3[] m_trajectoryData;

    int m_currentArrowSelection;
    bool m_isCharging;
    Vector2 m_dragStart;
    float m_currentPower;
    float m_lastPower = -1f;

    Arrow m_previewArrow;
    Quaternion m_lockedRotation;
    bool m_isRotationLocked;

    void Awake()
    {
        foreach (ArrowPool pool in m_arrowPools) pool.Setup();
        m_trajectoryBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)m_trajectoryPointCount, sizeof(float) * 3);
        m_trajectoryData = new Vector3[m_trajectoryPointCount];
        m_trajectoryEffect.SetGraphicsBuffer("Trajectory Buffer", m_trajectoryBuffer);
        m_trajectoryEffect.SetUInt("Valid Point Count", 0);
        m_mainCamera = Camera.main;
    }

    void OnEnable()
    {
        m_fireAction.action.Enable();
        m_fireAction.action.performed += StartCharging;
        m_fireAction.action.canceled += ReleaseFire;

        m_swapArrowAction.action.Enable();
        m_swapArrowAction.action.performed += SwapArrow;

        m_aimAction.action.Enable();
    }

    void OnDisable()
    {
        m_fireAction.action.performed -= StartCharging;
        m_fireAction.action.canceled -= ReleaseFire;
        m_fireAction.action.Disable();

        m_swapArrowAction.action.performed -= SwapArrow;
        m_swapArrowAction.action.Disable();

        m_aimAction.action.Disable();
    }

    void OnDestroy()
    {
        m_trajectoryBuffer?.Release();
    }

    void Update()
    {
        if (!m_isCharging && m_mainCamera is not null)
        {
            Vector3 mouseWorldPos = m_mainCamera.ScreenToWorldPoint(m_aimAction.action.ReadValue<Vector2>());
            Vector2 aimDir = transform.position - mouseWorldPos;
            if (aimDir.sqrMagnitude > 0f)
            {
                float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        if (m_isCharging && m_previewArrow is not null)
        {
            if (!m_isRotationLocked)
            {
                m_lockedRotation = transform.rotation;
                m_isRotationLocked = true;
            }

            Vector2 currentAimInput = m_aimAction.action.ReadValue<Vector2>();
            Vector2 drag = currentAimInput - m_dragStart;
            m_currentPower = Mathf.Clamp(drag.magnitude / 100f, 0f, m_maxPower);

            transform.rotation = m_lockedRotation;
            m_previewArrow.transform.rotation = m_lockedRotation;
            m_previewArrow.transform.position = m_arrowSpawnPoint.position;
            UpdateTrajectoryVFX();
            if (!(Mathf.Abs(m_currentPower - m_lastPower) > 0.01f)) return;
            m_lastPower = m_currentPower;
        }
        else
        {
            m_trajectoryEffect.SetUInt("Valid Point Count", 0);
        }
    }

    void StartCharging(InputAction.CallbackContext context)
    {
        m_isCharging = true;
        m_dragStart = m_aimAction.action.ReadValue<Vector2>();

        m_arrowPools[m_currentArrowSelection].Get(out m_previewArrow);
        m_previewArrow.transform.position = m_arrowSpawnPoint.position;
        m_previewArrow.transform.rotation = m_arrowSpawnPoint.rotation;
        m_previewArrow.SetPreview(true, m_playerCollider);

        m_isRotationLocked = false;
        m_currentPower = 0f;
        m_lastPower = -1f;
    }

    void ReleaseFire(InputAction.CallbackContext context)
    {
        if (!m_isCharging || m_previewArrow == null) return;

        m_isCharging = false;

        Vector2 currentAimInput = m_aimAction.action.ReadValue<Vector2>();
        Vector2 drag = currentAimInput - m_dragStart;
        m_currentPower = Mathf.Clamp(drag.magnitude / 100f, 0f, m_maxPower);

        m_previewArrow.SetPreview(false);
        m_previewArrow.Fire(m_arrowSpawnPoint.right, m_currentPower, m_playerCollider);

        m_previewArrow = null;
        m_currentPower = 0f;
        m_lastPower = -1f;
        m_isRotationLocked = false;

        m_trajectoryEffect.Reinit();
        m_trajectoryEffect.SetUInt("Valid Point Count", 0);
    }

    void UpdateTrajectoryVFX()
    {
        List<Vector2> points = m_previewArrow.CalculateTrajectory(m_arrowSpawnPoint, m_currentPower);
        
        if (points.Count == 0)
        {
            m_trajectoryEffect.SetUInt("Valid Point Count", 0);
            return;
        }

        for (int i = 0; i < m_trajectoryPointCount; i++)
        {
            if (i < points.Count)
                m_trajectoryData[i] = new Vector3(points[i].x, points[i].y, 0f);
            else
                m_trajectoryData[i] = m_trajectoryData[points.Count - 1];
        }

        m_trajectoryBuffer.SetData(m_trajectoryData);
        m_trajectoryEffect.SetUInt("Valid Point Count", (uint)points.Count);
        m_trajectoryEffect.SendEvent("Show");
    }

    void SwapArrow(InputAction.CallbackContext context)
    {
        float inputY = context.ReadValue<Vector2>().y;
        m_currentArrowSelection = (m_currentArrowSelection + Mathf.RoundToInt(inputY) + m_arrowPools.Count) % m_arrowPools.Count;
    }
}
