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

    void Awake()
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
    public void TakeHit()
    {
        if (!canMoveState) return;
        StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        canMove(false);
        animator.SetBool("isHit", true);

        yield return new WaitForSeconds(0.4f);

        animator.SetBool("isHit", false);
        canMove(true); 
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
}
