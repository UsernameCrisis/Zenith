using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    private InputAction moveAction;
    private Vector3 moveValue;

    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float groundDist;
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator animator;

    private float defaultMoveSpeed;
    private float idleTime = 0f;

    private bool canMoveState = true;
    private bool isDead = false;

    public void Initialize()
    {
        defaultMoveSpeed = moveSpeed;
        rb = gameObject.GetComponent<Rigidbody>();
        moveAction = InputSystem.actions.FindAction("Move");
    }

    void Update()
    {
        moveValue = moveAction.ReadValue<Vector2>();

        bool isRunning = moveValue.magnitude > 0.01f;
        animator.SetBool("isRunning", isRunning);

        if (!isRunning)
            idleTime += Time.deltaTime;
        else
            idleTime = 0f;

        animator.SetFloat("idleTime", idleTime);

        if (moveValue.x != 0)
            sr.flipX = moveValue.x < 0;

        RayCastToGround();
    }

    void FixedUpdate()
    {
        if (canMoveState)
            rb.AddForce(new Vector3(moveValue.x * moveSpeed, 0, moveValue.y * moveSpeed));
    }

    public void Die()
    {
        canMove(false);
        isDead = true;
        animator.SetBool("isDead", true);
        Debug.Log("Player died");
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
    public Animator getAnimator() { return animator; }
    public bool dead() { return isDead; }
}
