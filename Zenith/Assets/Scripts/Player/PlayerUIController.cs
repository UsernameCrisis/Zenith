using UnityEngine;

public class PlayerUIController : MonoBehaviour
{
    [SerializeField]
    private Canvas canvas;
    [SerializeField]
    private Camera camera;
    void Awake()
    {
        canvas.worldCamera = camera;
    }
}
