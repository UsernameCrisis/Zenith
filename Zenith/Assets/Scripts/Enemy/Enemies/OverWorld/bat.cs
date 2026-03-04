using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Bat : MonoBehaviour
{
    private enum EnemyState { Idle, Alert, Attack }

    [Header("Idle Behavior")]
    [SerializeField] private float desRadius = 5f;
    [SerializeField] private float minDistToDest = 1f;
    [SerializeField] private float minWait = 1f;
    [SerializeField] private float maxWait = 2f;
    [SerializeField] private float maxMoveDuration = 5f;

    [Header("Alert Behavior")]
    [SerializeField] private float visionRange = 7f;
    [SerializeField] private float aggroDuration = 10f;

    [Header("Combat")]
    [SerializeField] private int attackPower = 10;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackDuration = 1f;
    [SerializeField] private Transform player;

    [Header("Hit Detection")]
    [SerializeField] private CapsuleCollider attackCapsule;
    [SerializeField] private LayerMask playerLayer;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;

    private EnemyState currentState;
    private Vector3 destination;
    private Vector3 lastKnownPlayerPosition;

    private bool seePlayer;
    private bool isWaiting;
    private bool hasDealtDamage;

    private float originalSpeed;
    private float attackTimer;

    private float idleCheckTimer;
    private float idleWaitTimer;
    private float idleWaitDuration;
    private float moveTimeoutTimer;

    private float alertCheckTimer;
    private float aggroTimer;
    private float lookAroundTimer;
    private float pathUpdateTimer;

    private readonly float idleCheckInterval = 0.5f;
    private readonly float alertCheckInterval = 0.2f;
    private readonly float lookAroundCooldown = 2f;
    private readonly float pathUpdateInterval = 0.5f;

    private void Start()
    {
        originalSpeed = agent.speed;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        PickRandomDestination();
        ChangeState(EnemyState.Idle);
    }

    private void Update()
    {
        transform.rotation = Quaternion.identity;
        UpdateAnimations();

        if (currentState == EnemyState.Attack)
            FacePlayer();
        else
            FlipSpriteBasedOnMovement();

        switch (currentState)
        {
            case EnemyState.Idle: Idle(); break;
            case EnemyState.Alert: Alert(); break;
            case EnemyState.Attack: Attack(); break;
        }
    }

    private void ChangeState(EnemyState newState)
    {
        currentState = newState;

        if (newState == EnemyState.Attack)
        {
            agent.isStopped = true;
            agent.ResetPath();
            attackTimer = 0f;
            hasDealtDamage = false;
        }
        else
        {
            agent.isStopped = false;
        }
    }

    private void Attack()
    {
        attackTimer += Time.deltaTime;

        if (!hasDealtDamage && attackTimer >= 0.25f)
        {
            CheckForHit();
            hasDealtDamage = true;
        }

        if (attackTimer >= attackDuration)
        {
            ChangeState(EnemyState.Alert);
        }
    }

    private void CheckForHit()
    {
        if (attackCapsule == null) return;

        float halfHeight = (attackCapsule.height / 2f) - attackCapsule.radius;
        Vector3 point0 = attackCapsule.transform.TransformPoint(attackCapsule.center + Vector3.up * halfHeight);
        Vector3 point1 = attackCapsule.transform.TransformPoint(attackCapsule.center + Vector3.down * halfHeight);
        float worldRadius = attackCapsule.radius * Mathf.Max(attackCapsule.transform.lossyScale.x, attackCapsule.transform.lossyScale.z);

        Collider[] hitColliders = Physics.OverlapCapsule(point0, point1, worldRadius, playerLayer);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                PlayerOverworldAttributes pAttributes = hitCollider.GetComponent<PlayerOverworldAttributes>();
                if (pAttributes != null)
                {
                    pAttributes.TakeDamage(attackPower);
                }
            }
        }
    }

    private void UpdateAnimations()
    {
        if (currentState == EnemyState.Attack)
        {
            animator.SetInteger("state", 3);
        }
        else
        {
            if (agent.velocity.magnitude > 0.1f)
                animator.SetInteger("state", 2);
            else
                animator.SetInteger("state", 1);
        }
    }


    private void Idle()
    {
        idleCheckTimer += Time.deltaTime;
        if (idleCheckTimer >= idleCheckInterval)
        {
            idleCheckTimer = 0f;
            SeePlayerCheck();
        }

        if (seePlayer) { ChangeState(EnemyState.Alert); return; }

        if (!isWaiting && agent.hasPath)
        {
            moveTimeoutTimer += Time.deltaTime;
            if (moveTimeoutTimer >= maxMoveDuration) { agent.ResetPath(); PickRandomDestination(); }
        }

        if (!isWaiting && agent.remainingDistance <= minDistToDest && !agent.pathPending)
        {
            isWaiting = true;
            idleWaitDuration = Random.Range(minWait, maxWait);
            idleWaitTimer = 0f;
        }

        if (isWaiting)
        {
            idleWaitTimer += Time.deltaTime;
            if (idleWaitTimer >= idleWaitDuration) { isWaiting = false; PickRandomDestination(); }
        }
    }

    private void Alert()
    {
        alertCheckTimer += Time.deltaTime;
        pathUpdateTimer += Time.deltaTime;

        if (alertCheckTimer >= alertCheckInterval) { alertCheckTimer = 0f; SeePlayerCheck(); }

        if (seePlayer)
        {
            aggroTimer = 0f;
            if (Vector3.Distance(transform.position, player.position) <= attackRange)
            {
                ChangeState(EnemyState.Attack);
                return;
            }

            if (pathUpdateTimer >= pathUpdateInterval) { agent.SetDestination(player.position); pathUpdateTimer = 0f; }
            return;
        }

        lookAroundTimer += Time.deltaTime;
        if (lookAroundTimer >= lookAroundCooldown) { lookAroundTimer = 0f; StartCoroutine(LookAroundRoutine()); }

        aggroTimer += Time.deltaTime;
        if (aggroTimer >= aggroDuration) ChangeState(EnemyState.Idle);
    }


    private void PickRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * desRadius + transform.position;
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, desRadius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    private void SeePlayerCheck()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Vector3 facingDirection = transform.localScale.x > 0 ? Vector3.left : Vector3.right;

        if (Vector3.Angle(facingDirection, directionToPlayer) > 95f) { seePlayer = false; return; }

        if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, visionRange))
        {
            if (hit.transform == player) { seePlayer = true; lastKnownPlayerPosition = player.position; return; }
        }
        seePlayer = false;
    }

    private IEnumerator LookAroundRoutine()
    {
        transform.localScale = new Vector3(transform.localScale.x * -1f, transform.localScale.y, transform.localScale.z);
        yield return new WaitForSeconds(0.25f);
        if (seePlayer) yield break;
        transform.localScale = new Vector3(transform.localScale.x * -1f, transform.localScale.y, transform.localScale.z);
        agent.SetDestination(GuessPlayerPosition(5f));
    }

    private Vector3 GuessPlayerPosition(float dist)
    {
        Vector3 randomDir = Random.insideUnitSphere * dist;
        randomDir.y = 0f;
        randomDir += player.position;
        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, dist, NavMesh.AllAreas)) return hit.position;
        return transform.position;
    }

    private void FlipSpriteBasedOnMovement()
    {
        if (Mathf.Abs(agent.velocity.x) > 0.1f)
        {
            Vector3 scale = transform.localScale;
            scale.x = agent.velocity.x > 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void FacePlayer()
    {
        float xDir = player.position.x - transform.position.x;
        if (Mathf.Abs(xDir) > 0.1f)
        {
            Vector3 scale = transform.localScale;
            scale.x = xDir > 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
}