using UnityEngine.InputSystem;
using UnityEngine;
using System;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private LayerMask gridLayerMask;
    [SerializeField] private LayerMask selectionLayerMask;

    private Vector3 lastPosition;
    private Collider lastCollider;
    private InputAction mouseInputPosition, mouseInputLeftClick, escapeKeyAction;
    public event Action Onclicked, OnExit;
    public event Action<Collider> OnHoverEnter, OnHoverExit, OnColliderClicked;
    private bool selectMode = true;

    void Awake()
    {
        mouseInputPosition = InputSystem.actions.FindAction("MousePosition");
        mouseInputLeftClick = InputSystem.actions.FindAction("Attack"); // action "Attack" (left click) digunakan karena pakai default InputSystem
        escapeKeyAction = InputSystem.actions.FindAction("ExitSelect");
    }

    void Update()
    {
        HandleClickInputs();
        HandleHoverDetection();
    }

    private void HandleClickInputs()
    {
        if (EventSystem.current != null && IsPointerOverUI())
            return;

        if (mouseInputLeftClick.triggered)
        {
            Onclicked?.Invoke();

            Collider clickedCollider = RaycastHoverCollider();
            if (clickedCollider != null)
            {
                OnColliderClicked?.Invoke(clickedCollider);
            }
        }
            
        if (escapeKeyAction.ReadValue<float>() == 1)
            OnExit?.Invoke();
    }

    private void HandleHoverDetection()
    {
        if (EventSystem.current != null && IsPointerOverUI())
            return;

        Collider newCollider = RaycastHoverCollider();

        // Detect hover enter / exit
        if (newCollider != lastCollider)
        {
            if (lastCollider != null)
                OnHoverExit?.Invoke(lastCollider);

            if (newCollider != null)
                OnHoverEnter?.Invoke(newCollider);

            lastCollider = newCollider;
        }
    }
    
    private Collider RaycastHoverCollider()
    {
        Vector2 mouseScreenPos = mouseInputPosition.ReadValue<Vector2>();
        Ray ray = sceneCamera.ScreenPointToRay(mouseScreenPos);

        LayerMask currentMask = selectMode ? selectionLayerMask : gridLayerMask;

        if (Physics.Raycast(ray, out RaycastHit hit, 100, currentMask))
        {
            lastPosition = hit.point;
            return hit.collider;
        }

        return null;
    }

    public bool IsPointerOverUI() =>
        EventSystem.current.IsPointerOverGameObject();

    public Vector3 GetHoveredMapPosition() => lastPosition;

    public void SetSelectMode(bool value)
    {
        selectMode = value;
    }

    // public Collider GetHoveredCollider()
    // {
    //     Vector3 mousePos = new Vector3(mouseInputPosition.ReadValue<Vector2>().x, mouseInputPosition.ReadValue<Vector2>().y, 0);
    //     mousePos.z = sceneCamera.nearClipPlane;
    //     Ray ray = sceneCamera.ScreenPointToRay(mousePos);
    //     RaycastHit hit;
    //     if (Physics.Raycast(ray, out hit, 100, gridLayerMask))
    //     {
    //         lastCollider = hit.collider;
    //     }
    //     return lastCollider;
    // }
}
