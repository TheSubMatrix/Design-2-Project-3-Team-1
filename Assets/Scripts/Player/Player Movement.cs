using CustomNamespace.DependencyInjection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
public  class PlayerMovement : MonoBehaviour, IDependencyProvider, IPlayerMovementEventProvider
{
    static readonly int s_speed = Animator.StringToHash("Speed");

    [Provide]
    // ReSharper disable once UnusedMember.Local
    //This is used by the Dependency Injection Framework
    IPlayerMovementEventProvider ProvidePlayerMovement()
    {
        return this;
    }
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
    [SerializeField] Animator m_characterAnimator;
    Rigidbody2D m_connectedRB;
    Rigidbody2D m_previousConnectedRB;
    Vector2 m_connectionWorldPosition;
    Vector2 m_desiredMoveDirection;
    Vector2 m_modifiedVelocity;
    Vector2 m_connectionVelocity;
    Vector2 m_contactNormal;
    Vector2 m_steepNormal;
    Vector2 m_jumpDirection;
    bool m_desiresJump;
    uint m_groundedContacts;
    uint m_steepContacts;
    uint m_stepsSinceLastGrounded;
    uint m_stepsSinceLastJump;
    bool IsGrounded => m_groundedContacts > 0;
    bool OnSteep => m_steepContacts > 0;
    float GroundDotProduct => Mathf.Cos(m_groundAngle * Mathf.Deg2Rad);
    public event IPlayerMovementEventProvider.OnMove OnMoveEvent;
    public event IPlayerMovementEventProvider.OnJump OnJumpEvent;
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
        if (m_desiredMoveDirection.sqrMagnitude > 0.01f)
        {
            OnMoveEvent?.Invoke();
        }
    }
    void OnJump(InputAction.CallbackContext context)
    {
        m_desiresJump = true;
    }
    void FixedUpdate()
    {
        UpdateState();   
        AdjustVelocity();
        if (m_desiresJump)
        {
            Jump();
            m_desiresJump = false;
        }
        m_playerRB.linearVelocity = m_modifiedVelocity;
        ClearState();
    }
    void OnCollisionEnter2D(Collision2D other)
    {
        EvaluateCollision(other);
    }
    void OnCollisionStay2D(Collision2D other)
    {
        EvaluateCollision(other);
    }
    void ClearState()
    {
        m_modifiedVelocity = Vector2.zero;
        m_groundedContacts = 0;
        m_steepContacts = 0;
        m_contactNormal = Vector2.up;
        m_previousConnectedRB = m_connectedRB;
    }
    void AdjustVelocity()
    {
        
        float groundRateOfChange = m_desiredMoveDirection.x > m_playerRB.linearVelocity.x ? m_groundedAcceleration : m_groundedDeceleration;
        float airRateOfChange = m_desiredMoveDirection.x > m_playerRB.linearVelocity.x ? m_airAcceleration : m_airDeceleration;
        float rateOfChange = IsGrounded ? groundRateOfChange : airRateOfChange;
        Vector2 relativeVelocity = m_modifiedVelocity - m_connectionVelocity;
        Vector2 projectedVelocity = ProjectDirectionOnPlane(Vector2.right, m_contactNormal);
        float currentVelocity = Vector2.Dot(relativeVelocity, projectedVelocity);
        float newVelocity = Mathf.MoveTowards(currentVelocity, m_desiredMoveDirection.x, rateOfChange * Time.deltaTime);
        m_modifiedVelocity += projectedVelocity * (newVelocity - currentVelocity);
        m_characterAnimator.SetFloat(s_speed, currentVelocity);
    }
    void EvaluateCollision(Collision2D other)
    {
        foreach (ContactPoint2D contact in other.contacts)
        {
            if (contact.normal.y >= GroundDotProduct)
            {
                m_groundedContacts += 1;
                m_contactNormal = contact.normal;
                m_connectedRB = other.rigidbody;
            }
            else if (contact.normal.y > -0.01f)
            {
                m_steepContacts++;
                m_steepNormal += contact.normal;
                if (m_groundedContacts != 0) continue;
                m_connectedRB = other.rigidbody;
            }
        }
    }

    static Vector2 ProjectDirectionOnPlane(Vector2 vectorToProject, Vector2 normal)
    {
        return (vectorToProject - normal * Vector2.Dot(vectorToProject, normal)).normalized;
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
    void UpdateConnectionState()
    {
        if (m_connectedRB == m_previousConnectedRB)
        {
            Vector2 connectionMovement = m_connectedRB.position - m_connectionWorldPosition;
            m_connectionVelocity = connectionMovement / Time.deltaTime;
        }
        m_connectionWorldPosition = m_connectedRB.position;
    }

    void UpdateState()
    {
        m_stepsSinceLastGrounded += 1;
        m_stepsSinceLastJump += 1;
        m_modifiedVelocity = m_playerRB.linearVelocity;
        if (IsGrounded || SnapToGround() || CheckSteepContacts())
        {
            m_stepsSinceLastGrounded = 0;
            if (m_groundedContacts > 0)
            {
                m_contactNormal.Normalize();
            }
        }
        else
        {
            m_contactNormal = Vector2.up;
        }

        if (!m_connectedRB) return;
        if (m_connectedRB.bodyType == RigidbodyType2D.Kinematic || m_connectedRB.mass >= m_playerRB.mass)
        {
            UpdateConnectionState();
        }
    }

    void Jump()
    {
        if (IsGrounded)
        {
            m_jumpDirection = OnSteep ? m_steepNormal : m_contactNormal;
        }
        else
        {
            return;
        }
        OnJumpEvent?.Invoke();
        m_jumpDirection = (m_jumpDirection + Vector2.up).normalized;
        m_stepsSinceLastJump = 0;
        float jumpSpeed = Mathf.Sqrt(2f * Physics2D.gravity.magnitude * m_jumpHeight);
        float alignedSpeed = Vector2.Dot(m_modifiedVelocity, m_jumpDirection);
        if (alignedSpeed > 0)
        {
            jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0);
        }
        m_modifiedVelocity += m_contactNormal * jumpSpeed;
    }
    
}
