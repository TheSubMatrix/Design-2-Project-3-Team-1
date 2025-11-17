using System.Collections.Generic;
using CustomNamespace.DependencyInjection;
using CustomNamespace.GenericDatatypes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class Bow : MonoBehaviour
{
    [Provide]
    Bow ProvideBow() { return this; }
    [FormerlySerializedAs("m_arrowPools")]
    [Header("Arrow & Trajectory")]
    [SerializeField] List<Quiver> m_quivers;
    [SerializeField] Transform m_arrowSpawnPoint;
    [SerializeField] VisualEffect m_trajectoryEffect;
    [SerializeField] Collider2D m_playerCollider;
    [FormerlySerializedAs("chargeTime")]
    [SerializeField] float m_chargeTime = 0.5f;
    [SerializeField, Range(1, 50)] uint m_trajectoryPointCount = 20;
    [SerializeField, Range(0.1f, 3f)] float m_maxPower = 1f;

    [Header("Input Actions")]
    [SerializeField] InputActionReference m_fireAction;
    [FormerlySerializedAs("m_cancelAction")] [SerializeField] InputActionReference m_prepareShotAction;
    [SerializeField] InputActionReference m_swapArrowAction;
    [Header("Events")]
    [SerializeField] Observer<BowUIData> m_bowUI;
    
    GraphicsBuffer m_trajectoryBuffer;
    Vector3[] m_trajectoryData;
    bool m_isCharging;
    float m_currentChargeTime;
    float m_currentPower;
    int m_currentArrowSelection;
    Arrow m_previewArrow;
    
    
    // ReSharper disable once UnusedMember.Local
    // This actually gets called by the DI system
    [Inject]
    void InitializeQuivers(ILevelDataProvider levelData)
    {
        foreach (Quiver pool in m_quivers) pool.Setup(levelData);
    }
    void Start()
    {
        m_bowUI.Value ??= new BowUIData();
        m_bowUI.Update(data => data
            .WithArrowTypeName(m_quivers[m_currentArrowSelection].ArrowPrefab.NameForUI)
            .WithArrowUISprite(m_quivers[m_currentArrowSelection].ArrowPrefab.SpriteForUI)
            .WithAmmo(m_quivers[m_currentArrowSelection].CurrentAmmo));
        m_trajectoryBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)m_trajectoryPointCount, sizeof(float) * 3);
        m_trajectoryData = new Vector3[m_trajectoryPointCount];
        m_trajectoryEffect.SetGraphicsBuffer("Trajectory Buffer", m_trajectoryBuffer);
        m_trajectoryEffect.SetUInt("Valid Point Count", 0);
    }

    void OnEnable()
    {
        m_fireAction.action.Enable();
        m_fireAction.action.performed += ReleaseFire;

        m_prepareShotAction.action.Enable();    
        m_prepareShotAction.action.performed += StartCharging;
        m_prepareShotAction.action.canceled += OnShotCancelled;
        
        m_swapArrowAction.action.Enable();
        m_swapArrowAction.action.performed += SwapArrow;
    }

    void OnDisable()
    {
        m_fireAction.action.performed -= ReleaseFire;
        m_fireAction.action.Disable();
        
        m_prepareShotAction.action.performed -= StartCharging;
        m_prepareShotAction.action.canceled -= OnShotCancelled;
        m_prepareShotAction.action.Disable();       
        
        m_swapArrowAction.action.performed -= SwapArrow;
        m_swapArrowAction.action.Disable();
    }

    void OnDestroy()
    {
        m_trajectoryBuffer?.Release();
    }

    void Update()
    {
        if (m_isCharging && m_previewArrow is not null)
        {
            m_currentChargeTime += Time.deltaTime;
            m_currentPower = Mathf.Clamp01(m_currentChargeTime / m_chargeTime) * m_maxPower;
            m_previewArrow.transform.position = m_arrowSpawnPoint.position;
            m_previewArrow.transform.rotation = m_arrowSpawnPoint.rotation;
            UpdateTrajectoryVFX();
        }
        else
        {
            m_trajectoryEffect.SetUInt("Valid Point Count", 0);
        }
    }

    void StartCharging(InputAction.CallbackContext context)
    {
        m_isCharging = true;
        m_quivers[m_currentArrowSelection].Get(out m_previewArrow);
        m_currentPower = 0f;
        m_currentChargeTime = 0f;
        if (m_previewArrow == null)
        {
            m_isCharging = false;
            return;
        }
        m_previewArrow.transform.position = m_arrowSpawnPoint.position;
        m_previewArrow.transform.rotation = m_arrowSpawnPoint.rotation;
        m_previewArrow.SetPreview(true, m_playerCollider);
    }

    void ReleaseFire(InputAction.CallbackContext context)
    {
        if (!m_isCharging || m_previewArrow == null) return;
        m_isCharging = false;
        m_previewArrow.SetPreview(false);
        m_previewArrow.Fire(m_arrowSpawnPoint.right, m_currentPower, m_playerCollider);
        m_previewArrow = null;
        m_currentPower = 0f;
        m_currentChargeTime = 0f;
        m_trajectoryEffect.Reinit();
        m_trajectoryEffect.SetUInt("Valid Point Count", 0);
        m_bowUI.Update(data => data.WithAmmo(m_quivers[m_currentArrowSelection].CurrentAmmo));
    }
    void OnShotCancelled(InputAction.CallbackContext context)
    {
        if (!m_isCharging || m_previewArrow == null) return;
        m_isCharging = false;
        m_quivers[m_currentArrowSelection].ReleaseAndAddBack(m_previewArrow);
        m_previewArrow.SetPreview(false);
        m_previewArrow = null;
        m_currentPower = 0f;
        m_currentChargeTime = 0f;
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
        if (m_isCharging) return;
        float inputY = context.ReadValue<Vector2>().y;
        m_currentArrowSelection = (m_currentArrowSelection + Mathf.RoundToInt(inputY) + m_quivers.Count) % m_quivers.Count;
        m_bowUI.Update(data => data
            .WithArrowTypeName(m_quivers[m_currentArrowSelection].ArrowPrefab.NameForUI)
            .WithArrowUISprite(m_quivers[m_currentArrowSelection].ArrowPrefab.SpriteForUI)
            .WithAmmo(m_quivers[m_currentArrowSelection].CurrentAmmo));
    }
}
