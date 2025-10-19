using UnityEngine;
using UnityEngine.InputSystem;

public class CombatCameraMovement : MonoBehaviour
{
    private InputAction cameraMoveAction;
    private InputAction shiftInput;
    private Vector3 moveValue;
    private float isFast;
    private float moveSpeed;
    private Rigidbody rb;

    [SerializeField] private float defaultMoveSpeed = 70f;
    [SerializeField] private float speedMultiplier = 2f;
    void Awake()
    {
        moveSpeed = defaultMoveSpeed;
        cameraMoveAction = InputSystem.actions.FindAction("Move");
        shiftInput = InputSystem.actions.FindAction("Sprint"); // action "Sprint" karena pakai default InputSystem
        rb = gameObject.GetComponent<Rigidbody>();
    }

    void Update()
    {
        moveValue = cameraMoveAction.ReadValue<Vector2>();
        isFast = shiftInput.ReadValue<float>();

        if (isFast == 1)
            moveSpeed = defaultMoveSpeed * speedMultiplier;
        else
            moveSpeed = defaultMoveSpeed;
    }

    void FixedUpdate()
    {
        rb.AddForce(new Vector3(moveValue.x * moveSpeed, 0, moveValue.y * moveSpeed));
    }
}
