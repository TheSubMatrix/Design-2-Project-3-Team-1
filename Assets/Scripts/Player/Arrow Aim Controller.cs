using UnityEngine;
using UnityEngine.InputSystem;

public class ArrowAimController : MonoBehaviour
{
    [SerializeField] InputActionReference m_aimAction;
    Vector2 m_currentAimInput;
    Camera m_mainCamera;
    
    void Awake()
    {
        m_mainCamera = Camera.main;
        
        m_aimAction.action.performed += OnAim;
        m_aimAction.action.canceled += OnAim;
    }
    
    void OnEnable()
    {
        m_aimAction.action.Enable();
    }

    void OnDisable()
    {
        m_aimAction.action.Disable();
    }

    void OnAim(InputAction.CallbackContext context)
    {
        // Store the screen position reported by the cursor
        m_currentAimInput = context.ReadValue<Vector2>();
    }
    
    void Update()
    {
        if (m_mainCamera is null) return;
        Vector3 mouseWorldPosition = m_mainCamera.ScreenToWorldPoint(m_currentAimInput);
        Vector2 aimDirection = (mouseWorldPosition - transform.position);
        if (!(aimDirection.magnitude > 0)) return;
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}