using UnityEngine;
[RequireComponent(typeof(Canvas))]
public class UICameraBinder : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Canvas>().worldCamera = Camera.main;
    }
}
