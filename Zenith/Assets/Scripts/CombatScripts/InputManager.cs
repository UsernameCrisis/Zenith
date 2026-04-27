using UnityEngine.InputSystem;
using UnityEngine;
using System;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private LayerMask gridLayerMask;
    [SerializeField] private LayerMask selectionLayerMask;
    [SerializeField] private LayerMask enemyLayerMask;

    private Vector3 lastPosition;
    private Collider lastCollider;
    private bool selectMode = true;
    private InputAction mouseInputPosition, mouseInputLeftClick, escapeKeyAction;
    public event Action Onclicked, OnExit, OnPauseMenu;
    public event Action<Collider> OnHoverEnter, OnHoverExit, OnColliderClicked;

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
        if (escapeKeyAction.triggered)
            OnExit?.Invoke();
            
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

        LayerMask currentMask = selectMode ? selectionLayerMask : (gridLayerMask | enemyLayerMask);

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

    public void SetSelectMode(bool value) => selectMode = value;
    public bool GetSelectMode() => selectMode;
}
