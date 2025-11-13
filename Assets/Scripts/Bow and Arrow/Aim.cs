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
    private void Awake()
    {
        m_mainCamera = Camera.main;
    }
    // Update is called once per frame
    void Update()
    {
        if (m_mainCamera is null) return;
        Vector3 mouseWorldPos = m_mainCamera.ScreenToWorldPoint(m_aimAction.action.ReadValue<Vector2>());
        Vector2 aimDir = transform.position - mouseWorldPos;
        if (!(aimDir.sqrMagnitude > 0f)) return;
        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        m_characterTransform.rotation = Quaternion.Euler(m_characterTransform.rotation.eulerAngles.x, GetRotationFromAngle(angle) ,m_characterTransform.rotation.eulerAngles.z);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        m_characterAnimator?.SetFloat(m_xAimAnimationParameter, aimDir.x);
        m_characterAnimator?.SetFloat(m_yAimAnimationParameter, aimDir.y);
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
