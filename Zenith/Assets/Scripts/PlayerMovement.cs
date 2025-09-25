using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private InputAction moveAction;
    private Vector3 moveValue;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float groundDist;
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Rigidbody rb;
    private float defaultMoveSpeed;

    void Awake()
    {
        defaultMoveSpeed = moveSpeed;
        rb = gameObject.GetComponent<Rigidbody>();
        moveAction = InputSystem.actions.FindAction("Move");
    }

    void Update()
    {
        moveValue = moveAction.ReadValue<Vector2>();
        RayCastToGround();
    }

    void FixedUpdate()
    {
        rb.AddForce(new Vector3(moveValue.x * moveSpeed, 0, moveValue.y * moveSpeed));
    }

    private void RayCastToGround()
    {
        RaycastHit hit;
        Vector3 castPos = transform.position;
        castPos.y += 1;

        if (Physics.Raycast(castPos, -transform.up, out hit, Mathf.Infinity, terrainLayer))
        {
            if (hit.collider != null)
            {
                Vector3 movePos = transform.position;
                movePos.y = hit.point.y + groundDist;
                transform.position = movePos;
            }
        }
    }
    public void canMove(bool canMove)
    {
        if (canMove)
        {
            moveSpeed = defaultMoveSpeed;
        }
        else
        {
            moveSpeed = 0;
        }
    }
}
