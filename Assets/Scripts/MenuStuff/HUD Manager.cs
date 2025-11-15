using UnityEngine;

public class HUDManager : MonoBehaviour
{
    public void UpdateSelectedArrow(BowUIData data)
    {
        Debug.Log(data.CurrentAmmo);
    }
    
}
