using UnityEngine;
[RequireComponent(typeof(Animator))]
public class Door : MonoBehaviour
{
    [SerializeField] string m_openAnimationTrigger;
    [SerializeField] string m_closeAnimationTrigger;
    Animator m_animator;
    void Awake()
    {
        m_animator = GetComponent<Animator>();
    }
    public void Open()
    {
        m_animator.SetTrigger(m_openAnimationTrigger);
    }
    public void Close()
    {
        m_animator.SetTrigger(m_closeAnimationTrigger);
    }
    public void ChangeOpenState(bool isOpen)
    {
        if (isOpen) Open();
        else Close();
    }
}
