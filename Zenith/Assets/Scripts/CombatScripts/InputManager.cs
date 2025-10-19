using UnityEngine.InputSystem;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private LayerMask gridLayerMask;

    private Vector3 lastPosition;
    private InputAction mouseInputPosition;

    void Awake()
    {
        mouseInputPosition = InputSystem.actions.FindAction("MousePosition");
    }

    public Vector3 GetSelectedMapPosition()
    {
        Vector3 mousePos = new Vector3(mouseInputPosition.ReadValue<Vector2>().x, mouseInputPosition.ReadValue<Vector2>().y, 0);
        mousePos.z = sceneCamera.nearClipPlane;
        Ray ray = sceneCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100, gridLayerMask))
        {
            lastPosition = hit.point;
        }
        return lastPosition;
    }
}
