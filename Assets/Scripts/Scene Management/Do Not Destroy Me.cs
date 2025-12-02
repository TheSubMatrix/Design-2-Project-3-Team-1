using UnityEngine;

public class DoNotDestroyMe : MonoBehaviour
{
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
}
