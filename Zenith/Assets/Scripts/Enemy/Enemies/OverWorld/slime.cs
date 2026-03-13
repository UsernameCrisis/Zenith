using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Slime : EnemyTrigger
{
    private enum EnemyState { Idle, Alert }

    [Header("Idle Behavior")]
    [SerializeField] private float desRadius = 5f;
    [SerializeField] private float minDistToDest = 1f;
    [SerializeField] private float minWait = 1f;
    [SerializeField] private float maxWait = 2f;
    [SerializeField] private float maxMoveDuration = 5f;

    [Header("Alert Behavior")]
    [SerializeField] private float visionRange = 7f;
    [SerializeField] private float aggroDuration = 10f;
    [SerializeField] private float runDistance = 5f;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;

    private EnemyState currentState;
    private bool seePlayer;
    private bool isWaiting;

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
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        PickRandomDestination();
        ChangeState(EnemyState.Idle);
    }

    private void Update()
    {
        transform.rotation = Quaternion.identity;
        UpdateAnimations();
        FlipSpriteBasedOnMovement();

        switch (currentState)
        {
            case EnemyState.Idle: Idle(); break;
            case EnemyState.Alert: Alert(); break;
        }
    }

    private void ChangeState(EnemyState newState)
    {
        currentState = newState;
        agent.isStopped = false;
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

            if (pathUpdateTimer >= pathUpdateInterval)
            {
                Vector3 moveDirection = transform.position - player.position;
                Vector3 runToPos = transform.position + moveDirection.normalized * runDistance;

                if (NavMesh.SamplePosition(runToPos, out NavMeshHit hit, runDistance, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }

                pathUpdateTimer = 0f;
            }
            return;
        }

        lookAroundTimer += Time.deltaTime;
        if (lookAroundTimer >= lookAroundCooldown) { lookAroundTimer = 0f; StartCoroutine(LookAroundRoutine()); }

        aggroTimer += Time.deltaTime;
        if (aggroTimer >= aggroDuration) ChangeState(EnemyState.Idle);
    }

    private void UpdateAnimations()
    {
        if (agent.velocity.magnitude > 0.1f)
            animator.SetInteger("state", 2);
        else
            animator.SetInteger("state", 1);
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
            if (hit.transform == player) { seePlayer = true; return; }
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
}