using UnityEngine;

public class DestroySelf : MonoBehaviour
{
    public void DestroyPrefab()
    {
        Destroy(gameObject);
    }
}