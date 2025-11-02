using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class Bow : MonoBehaviour
{
    [SerializeField] List<ArrowPool> m_arrowPools;
    [SerializeField, RequiredField] InputActionReference m_fireAction;
    [SerializeField, RequiredField] InputActionReference m_swapArrowAction;
    [SerializeField, RequiredField] Transform m_arrowSpawnPoint;
    [SerializeField, RequiredField] VisualEffect m_trajectoryEffect;
    [SerializeField] uint m_trajectoryPointCount = 20;
    GraphicsBuffer m_trajectoryBuffer;
    Vector3[] m_trajectoryData;
    int m_currentArrowSelection;
    void OnEnable()
    {
        m_fireAction.action.Enable();
        m_fireAction.action.performed += Fire;
        m_swapArrowAction.action.Enable();
        m_swapArrowAction.action.performed += SwapArrow;
    }
    void OnDisable()
    {
        m_fireAction.action.Disable();
        m_fireAction.action.performed -= Fire;
        m_swapArrowAction.action.Disable();
        m_swapArrowAction.action.performed -= SwapArrow;
    }

    void OnDestroy()
    {
        m_trajectoryBuffer.Release();
    }
    void Awake()
    {
        foreach (ArrowPool pool in m_arrowPools)
        {
            pool.Setup();
        }
        m_trajectoryBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)m_trajectoryPointCount, sizeof(float) * 3);
        m_trajectoryData = new Vector3[m_trajectoryPointCount];
        m_trajectoryEffect.SetGraphicsBuffer("Trajectory Buffer", m_trajectoryBuffer);
    }
    void Fire(InputAction.CallbackContext context)
    {
        m_arrowPools[m_currentArrowSelection].Get(out Arrow arrow);
        arrow.transform.position = m_arrowSpawnPoint.position;
        arrow.transform.rotation = m_arrowSpawnPoint.rotation;
        arrow.Fire(arrow.transform.right);
        ShowTrajectory(arrow, m_arrowSpawnPoint);
    }

    void SwapArrow(InputAction.CallbackContext context)
    {
        float inputY = context.ReadValue<Vector2>().y;
        m_currentArrowSelection = (m_currentArrowSelection + Mathf.RoundToInt(inputY) + m_arrowPools.Count) % m_arrowPools.Count;
    }
    void ShowTrajectory(Arrow arrow, Transform startTransform)
    {
        List<Vector2> trajectoryPoints = arrow.CalculateTrajectory(startTransform);
        int validPointCount = trajectoryPoints.Count;
        for (int i = 0; i < m_trajectoryPointCount; i++)
        {
            if (i < validPointCount)
            {
                m_trajectoryData[i] = new Vector3(trajectoryPoints[i].x, trajectoryPoints[i].y, 0);
            }
            else
            {
                m_trajectoryData[i] = m_trajectoryData[validPointCount - 1];
            }
        }
        m_trajectoryBuffer.SetData(m_trajectoryData);
        m_trajectoryEffect.SetUInt("Valid Point Count", (uint)validPointCount);
        m_trajectoryEffect.SendEvent("Show");
    }
    
}