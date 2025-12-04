using UnityEngine;
using UnityEngine.InputSystem;

public class Aim : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] InputActionReference m_aimAction;

    [Header("Character Rotation")]
    [SerializeField] Transform m_characterTransform;
    [SerializeField] float m_upRotation = 180;
    [SerializeField] float m_downRotation = 180;
    [SerializeField] float m_leftRotation = 90;
    [SerializeField] float m_rightRotation = 270;

    [Header("Animations")]
    [SerializeField] Animator m_characterAnimator;
    [SerializeField] string m_xAimAnimationParameter = "X Aim";
    [SerializeField] string m_yAimAnimationParameter = "Y Aim";

    Camera m_mainCamera;

    void OnEnable()
    {
        m_aimAction.action.Enable();
    }

    void OnDisable()
    {
        m_aimAction.action.Disable();
    }

    void Awake()
    {
        m_mainCamera = Camera.main;
    }

    void Update()
    {
        if (m_mainCamera is null) return;

        Vector2 mouseScreenPos = m_aimAction.action.ReadValue<Vector2>();
        Vector3 screenPos = m_mainCamera.WorldToScreenPoint(transform.position);
        screenPos.x = mouseScreenPos.x;
        screenPos.y = mouseScreenPos.y;
        Vector3 mouseWorldPos = m_mainCamera.ScreenToWorldPoint(screenPos);
        mouseScreenPos.x = transform.position.x - mouseWorldPos.x;
        mouseScreenPos.y = transform.position.y - mouseWorldPos.y;
        mouseScreenPos.Normalize();

        if (!(mouseScreenPos.sqrMagnitude > 0f)) return;

        float angle = Mathf.Atan2(mouseScreenPos.y, mouseScreenPos.x) * Mathf.Rad2Deg;

        m_characterTransform.rotation = Quaternion.Euler(m_characterTransform.rotation.eulerAngles.x, GetRotationFromAngle(angle), m_characterTransform.rotation.eulerAngles.z);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (!m_characterAnimator) return;

        m_characterAnimator.SetFloat(m_xAimAnimationParameter, mouseScreenPos.x);
        m_characterAnimator.SetFloat(m_yAimAnimationParameter, mouseScreenPos.y);
    }

    float GetRotationFromAngle(float angle)
    {
        angle = (angle + 360f) % 360f;

        switch (angle)
        {
            case <= 90f:
            {
                // Between right (0°) and up (90°)
                float t = angle / 90f;
                return Mathf.LerpAngle(m_rightRotation, m_upRotation, t);
            }
            case <= 180f:
            {
                // Between up (90°) and left (180°)
                float t = (angle - 90f) / 90f;
                return Mathf.LerpAngle(m_upRotation, m_leftRotation, t);
            }
            case <= 270f:
            {
                // Between left (180°) and down (270°)
                float t = (angle - 180f) / 90f;
                return Mathf.LerpAngle(m_leftRotation, m_downRotation, t);
            }
            default:
            {
                // Between down (270°) and right (360°/0°)
                float t = (angle - 270f) / 90f;
                return Mathf.LerpAngle(m_downRotation, m_rightRotation, t);
            }
        }
    }
}