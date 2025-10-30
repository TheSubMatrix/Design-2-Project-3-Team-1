using System;
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
    
    Vector2 m_desiredMoveDirection;
    Vector2 m_modifiedVelocity;
    Vector2 m_contactNormal;
    bool m_desiresJump;
    uint m_groundedContacts = 0;
    bool IsGrounded => m_groundedContacts > 0;
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
        Debug.Log(m_desiredMoveDirection);
    }
    void OnJump(InputAction.CallbackContext context)
    {
        m_desiresJump = true;
    }
    void FixedUpdate()
    {
        m_modifiedVelocity = m_playerRB.linearVelocity;
        AdjustMovementVelocity();
        if (m_desiresJump)
        {
            if (IsGrounded)
            {
                float jumpSpeed = Mathf.Sqrt(-2f * Physics2D.gravity.y * m_jumpHeight);
                float alignedSpeed = Vector2.Dot(m_modifiedVelocity, m_contactNormal);
                if (alignedSpeed > 0)
                {
                    jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0);
                }
                m_modifiedVelocity += m_contactNormal * jumpSpeed;
            }
            m_desiresJump = false;
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
}
