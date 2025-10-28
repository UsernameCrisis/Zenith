using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    private InputAction moveAction;
    private InputAction sprintAction;
    private Vector3 moveValue;

    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float runMultiplier = 2f;
    [SerializeField] private float groundDist;
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private SpriteRenderer sr;
    public Rigidbody rb;
    private float defaultMoveSpeed;
    private float idleTime = 0f;

    private bool canMoveState = true;
    private bool isDead = false;

    void Awake()
    {
        defaultMoveSpeed = moveSpeed;
        rb = gameObject.GetComponent<Rigidbody>();
        moveAction = InputSystem.actions.FindAction("Move");
        sprintAction = InputSystem.actions.FindAction("Sprint");
    }

    void Update()
    {
        if (isDead) return;
        moveValue = moveAction.ReadValue<Vector2>();

        bool isMoving = IsMoving();
        bool isSprinting = IsSprinting();
        float speedMultiplier = isSprinting ? runMultiplier : 1f;
        moveValue = moveValue * speedMultiplier;

        if (!isMoving)
            idleTime += Time.deltaTime;
        else
            idleTime = 0f;

        GameManager.Instance.Player.GetComponent<PlayerAnimationManager>().Animator.SetFloat("idleTime", idleTime);

        if (moveValue.x != 0)
            sr.flipX = moveValue.x < 0;

        RayCastToGround();
    }

    void FixedUpdate()
    {
        if (canMoveState)
            rb.AddForce(new Vector3(moveValue.x * moveSpeed, 0, moveValue.y * moveSpeed));
    }

    public IEnumerator HitRoutine()
    {
        canMove(false);
        GameManager.Instance.Player.GetComponent<PlayerAnimationManager>().Animator.SetBool("isHit", true);

        yield return new WaitForSeconds(0.4f);

        GameManager.Instance.Player.GetComponent<PlayerAnimationManager>().Animator.SetBool("isHit", false);
        if (!isDead)
        {
            canMove(true);
        }
    }

    public void canMove(bool canMove)
    {
        canMoveState = canMove;
        moveSpeed = canMove ? defaultMoveSpeed : 0;
        rb.linearVelocity = Vector3.zero;
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

    public bool IsMoving() { return moveValue.magnitude > 0.01f; }
    public bool IsSprinting() { return sprintAction.IsPressed(); }
}
