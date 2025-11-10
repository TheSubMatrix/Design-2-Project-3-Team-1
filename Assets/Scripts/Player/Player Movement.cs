using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerMovement : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField, RequiredField] InputActionReference m_moveAction;
    [SerializeField, RequiredField] InputActionReference m_jumpAction;
    [SerializeField, RequiredField] Rigidbody2D m_playerRB;
    [Header("Movement Settings" )]
    [SerializeField]float m_moveSpeed = 10f;
    [FormerlySerializedAs("m_acceleration")]
    [SerializeField] float m_groundedAcceleration = 15f;
    [SerializeField] float m_airAcceleration = 5f;
    [FormerlySerializedAs("m_deceleration")]
    [SerializeField]float m_groundedDeceleration = 20f;
    [SerializeField] float m_airDeceleration = 10f;
    [SerializeField] float m_groundAngle = 45f;
    [Header("Jump Settings")]
    [SerializeField] float m_jumpHeight = 2;
    [Header("Snap Settings")]
    [SerializeField] float m_snapDistance = 0.1f;
    [SerializeField] float m_maxSnapSpeed = 100f;
    [SerializeField] LayerMask m_snapLayers;
    
    Vector2 m_desiredMoveDirection;
    Vector2 m_modifiedVelocity;
    Vector2 m_contactNormal;
    Vector2 m_steepNormal;
    bool m_desiresJump;
    uint m_groundedContacts;
    uint m_steepContacts;
    uint m_stepsSinceLastGrounded;
    uint m_stepsSinceLastJump;
    bool IsGrounded => m_groundedContacts > 0;
    bool OnSteep => m_steepContacts > 0;
    float GroundDotProduct => Mathf.Cos(m_groundAngle * Mathf.Deg2Rad);
    
    void OnEnable()
    {
        m_moveAction.action.Enable();
        m_moveAction.action.started += OnMove;
        m_moveAction.action.canceled += OnMove;
        m_jumpAction.action.Enable();
        m_jumpAction.action.performed += OnJump;
    }
    void OnDisable()
    {
        m_moveAction.action.Disable();
        m_moveAction.action.started -= OnMove;
        m_moveAction.action.canceled -= OnMove;
        m_jumpAction.action.Disable();
        m_jumpAction.action.performed -= OnJump;
    }
    void OnMove(InputAction.CallbackContext context)
    {
        m_desiredMoveDirection = context.ReadValue<Vector2>() * m_moveSpeed;
    }
    void OnJump(InputAction.CallbackContext context)
    {
        m_desiresJump = true;
    }
    void FixedUpdate()
    {
        m_stepsSinceLastGrounded += 1;
        m_stepsSinceLastJump += 1;
        m_modifiedVelocity = m_playerRB.linearVelocity;
        AdjustMovementVelocity();
        if (IsGrounded || SnapToGround() || CheckSteepContacts())
        {
            m_stepsSinceLastGrounded = 0;
            if (m_desiresJump)
            {
                m_stepsSinceLastJump = 0;
                float jumpSpeed = Mathf.Sqrt(-2f * Physics2D.gravity.y * m_jumpHeight);
                float alignedSpeed = Vector2.Dot(m_modifiedVelocity, m_contactNormal);
                if (alignedSpeed > 0)
                {
                    jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0);
                }
                m_modifiedVelocity += m_contactNormal * jumpSpeed;
                m_desiresJump = false;
            }
        }
        m_playerRB.linearVelocity = m_modifiedVelocity;
        ResetStates();
    }
    void OnCollisionEnter2D(Collision2D other)
    {
        EvaluateCollision(other);
    }
    void OnCollisionStay2D(Collision2D other)
    {
        EvaluateCollision(other);
    }
    void ResetStates()
    {
        m_modifiedVelocity = Vector2.zero;
        m_groundedContacts = 0;
        m_steepContacts = 0;
        m_contactNormal = Vector2.up;
    }
    void AdjustMovementVelocity()
    {
        float groundRateOfChange = m_desiredMoveDirection.x > m_playerRB.linearVelocity.x ? m_groundedAcceleration : m_groundedDeceleration;
        float airRateOfChange = m_desiredMoveDirection.x > m_playerRB.linearVelocity.x ? m_airAcceleration : m_airDeceleration;
        float rateOfChange = IsGrounded ? groundRateOfChange : airRateOfChange;
        Vector2 projectedVelocity = ProjectOnContactPlane(Vector2.right).normalized;
        float currentVelocity = Vector2.Dot(m_modifiedVelocity, projectedVelocity);
        float newVelocity = Mathf.MoveTowards(currentVelocity, m_desiredMoveDirection.x, rateOfChange * Time.deltaTime);
        m_modifiedVelocity += projectedVelocity * (newVelocity - currentVelocity);
    }
    void EvaluateCollision(Collision2D other)
    {
        foreach (ContactPoint2D contact in other.contacts)
        {
            if (!(contact.normal.y >= GroundDotProduct)) continue;
            m_groundedContacts += 1;
            m_contactNormal = contact.normal;
        }
    }
    Vector2 ProjectOnContactPlane(Vector2 vectorToProject)
    {
        return vectorToProject - m_contactNormal * Vector3.Dot(vectorToProject, m_contactNormal);
    }
    bool SnapToGround()
    {
        if (m_stepsSinceLastGrounded > 1 || m_stepsSinceLastJump <= 2)
        {
            return false;
        }
        float speed = m_modifiedVelocity.magnitude;
        if (speed > m_maxSnapSpeed)
        {
            return false;   
        }
        RaycastHit2D hit = Physics2D.Raycast(m_playerRB.position, Vector2.down, m_snapDistance, m_snapLayers);
        if (hit.collider is null || hit.normal.y < GroundDotProduct)
        {
            return false;
        }
        else if (hit.normal.y > -0.01f)
        {
            m_steepContacts++;
            m_steepNormal += hit.normal;
        }
        m_contactNormal = hit.normal;
        m_groundedContacts = 1;
        
        float dot = Vector2.Dot(m_modifiedVelocity, m_contactNormal);
        if (dot > 0)
        {
            m_modifiedVelocity = (m_modifiedVelocity - hit.normal * dot).normalized * speed;
        }
        return true;
    }

    bool CheckSteepContacts()
    {
        if (m_steepContacts <= 1) return false;
        m_steepNormal.Normalize();
        if (!(m_steepNormal.y >= GroundDotProduct)) return false;
        m_groundedContacts = 1;
        m_contactNormal = m_steepNormal;
        return true;
    }
}
