using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    
    Rigidbody2D m_rb;
    
    void Awake()
    {
        m_rb = GetComponent<Rigidbody2D>();
    }
    void FixedUpdate()
    {
        m_rb.MovePosition(transform.position + (transform.right * (Mathf.Sin(Time.time) * Time.deltaTime)));
    }
}
