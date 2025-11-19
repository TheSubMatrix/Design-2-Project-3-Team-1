using AudioSystem;
using UnityEngine;
[RequireComponent(typeof(Animator))]
public class Door : MonoBehaviour
{
    [SerializeField] string m_openAnimationTrigger;
    [SerializeField] string m_closeAnimationTrigger;
    [SerializeField] SoundData m_openSound;
    [SerializeField] SoundData m_closeSound;
    Animator m_animator;
    void Awake()
    {
        m_animator = GetComponent<Animator>();
    }
    public void Open()
    {
        m_animator.SetTrigger(m_openAnimationTrigger);
        SoundManager.Instance.CreateSound().WithSoundData(m_openSound).WithPosition(transform.position).WithRandomPitch().Play();
    }
    public void Close()
    {
        m_animator.SetTrigger(m_closeAnimationTrigger);
        SoundManager.Instance.CreateSound().WithSoundData(m_closeSound).WithPosition(transform.position).WithRandomPitch().Play();
    }
    public void ChangeOpenState(bool isOpen)
    {
        if (isOpen) Open();
        else Close();
    }
}
