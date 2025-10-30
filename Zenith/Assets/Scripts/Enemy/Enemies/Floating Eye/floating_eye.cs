using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class FloatingEye : MonoBehaviour
{
    private enum EnemyState { Idle, Alert, Attack, Reload }

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
    [SerializeField] private int maxAmmo = 5;
    [SerializeField] private float attackCooldown = 1.1f;
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private Transform player;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private NavMeshAgent agent;

    private EnemyState currentState;
    private Vector3 destination;
    private Vector3 lastKnownPlayerPosition;

    private int ammo;
    private bool seePlayer;
    private bool isWaiting;
    private bool isReloading;

    private float attackCooldownTimer;
    private float originalSpeed;

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
        ammo = maxAmmo;
        originalSpeed = agent.speed;
        PickRandomDestination();
        ChangeState(EnemyState.Idle);
    }

    private void Update()
    {
        transform.rotation = Quaternion.identity;

        if (currentState == EnemyState.Attack)
            FacePlayer();
        else
            FlipSpriteBasedOnMovement();

        switch (currentState)
        {
            case EnemyState.Idle: Idle(); break;
            case EnemyState.Alert: Alert(); break;
            case EnemyState.Attack: Attack(); break;
            case EnemyState.Reload: Reload(); break;
        }
    }

    private void ChangeState(EnemyState newState)
    {
        currentState = newState;
        agent.speed = (newState == EnemyState.Attack || newState == EnemyState.Reload)
            ? originalSpeed * 0.7f
            : originalSpeed;

        int stateInt = newState switch
        {
            EnemyState.Idle => 1,
            EnemyState.Alert => 2,
            EnemyState.Attack => 3,
            EnemyState.Reload => 4,
            _ => 1
        };

        animator.SetInteger("state", stateInt);
    }

    private void Idle()
    {
        idleCheckTimer += Time.deltaTime;

        if (idleCheckTimer >= idleCheckInterval)
        {
            idleCheckTimer = 0f;
            SeePlayerCheck();
        }

        if (seePlayer)
        {
            ChangeState(EnemyState.Alert);
            return;
        }

        if (!isWaiting && agent.hasPath)
        {
            moveTimeoutTimer += Time.deltaTime;
            if (moveTimeoutTimer >= maxMoveDuration)
            {
                moveTimeoutTimer = 0f;
                agent.ResetPath();
                PickRandomDestination();
            }
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
            if (idleWaitTimer >= idleWaitDuration)
            {
                isWaiting = false;
                PickRandomDestination();
                moveTimeoutTimer = 0f;
            }
        }
    }

    private void Alert()
    {
        alertCheckTimer += Time.deltaTime;
        pathUpdateTimer += Time.deltaTime;

        if (alertCheckTimer >= alertCheckInterval)
        {
            alertCheckTimer = 0f;
            SeePlayerCheck();
        }

        if (seePlayer)
        {
            aggroTimer = 0f;
            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= attackRange)
            {
                ChangeState(EnemyState.Attack);
                return;
            }

            if (pathUpdateTimer >= pathUpdateInterval)
            {
                agent.SetDestination(player.position);
                pathUpdateTimer = 0f;
            }
            return;
        }

        lookAroundTimer += Time.deltaTime;
        if (lookAroundTimer >= lookAroundCooldown)
        {
            lookAroundTimer = 0f;
            StartCoroutine(LookAroundRoutine());
        }

        if (!agent.hasPath && !seePlayer && pathUpdateTimer >= pathUpdateInterval)
        {
            lastKnownPlayerPosition = GuessPlayerPosition(5f);
            agent.SetDestination(lastKnownPlayerPosition);
            pathUpdateTimer = 0f;
        }

        aggroTimer += Time.deltaTime;
        if (aggroTimer >= aggroDuration)
            ChangeState(EnemyState.Idle);
    }

    private void Attack()
    {
        pathUpdateTimer += Time.deltaTime;
        attackCooldownTimer -= Time.deltaTime;

        if (attackCooldownTimer <= 0f && ammo > 0)
        {
            FireProjectile();
            attackCooldownTimer = attackCooldown;
        }
        else if (ammo <= 0)
        {
            ChangeState(EnemyState.Reload);
        }

        MaintainAttackDistance();
    }

    private void Reload()
    {
        pathUpdateTimer += Time.deltaTime;
        MaintainReloadDistance();

        if (!isReloading)
        {
            isReloading = true;
            StartCoroutine(ReloadRoutine());
        }
    }

    private void PickRandomDestination() 
    { 
        Vector3 randomDirection = Random.insideUnitSphere * desRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, desRadius, NavMesh.AllAreas))
        { 
            destination = hit.position; agent.SetDestination(destination); 
        } 
    }

    private IEnumerator ReloadRoutine()
    {
        yield return new WaitForSeconds(1.45f);
        ammo = maxAmmo;
        isReloading = false;
        ChangeState(EnemyState.Alert);
    }

    private void FireProjectile()
    {
        if (currentState != EnemyState.Attack || ammo <= 0) return;

        ammo--;
        attackCooldownTimer = attackCooldown;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Vector3 offset = new(0f, 0.375f, 0f);
        Vector3 targetPosition = player.position + offset;
        Vector3 direction = (targetPosition - firePoint.position).normalized;

        Projectile projectile = proj.GetComponent<Projectile>();
        projectile.Initialize(direction);
        projectile.IgnoreShooter(GetComponent<Collider>());
    }

    private void SeePlayerCheck()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Vector3 origin = transform.position;
        Vector3 facingDirection = transform.localScale.x < 0 ? Vector3.left : Vector3.right;
        float angleToPlayer = Vector3.Angle(facingDirection, directionToPlayer);

        if (angleToPlayer > 95f)
        {
            seePlayer = false;
            return;
        }

        if (Physics.Raycast(origin, directionToPlayer, out RaycastHit hit, visionRange))
        {
            if (hit.transform == player)
            {
                seePlayer = true;
                lastKnownPlayerPosition = player.position;
                return;
            }
        }

        seePlayer = false;
    }

    private IEnumerator LookAroundRoutine()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
        yield return new WaitForSeconds(0.25f);

        if (seePlayer) yield break;

        scale.x *= -1f;
        transform.localScale = scale;
        yield return new WaitForSeconds(0.25f);

        lastKnownPlayerPosition = GuessPlayerPosition(5f);
        agent.SetDestination(lastKnownPlayerPosition);
        lookAroundTimer = 0;
    }

    private void MaintainAttackDistance()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (pathUpdateTimer < pathUpdateInterval) return;

        if (distance < attackRange - 0.5f)
        {
            Vector3 retreatPos = FindRetreatPosition(player.position, attackRange);
            if (retreatPos != Vector3.zero)
            {
                agent.SetDestination(retreatPos);
                pathUpdateTimer = 0f;
            }
        }
        else if (distance > attackRange + 0.5f)
        {
            agent.SetDestination(player.position);
            pathUpdateTimer = 0f;
        }
        else
        {
            agent.ResetPath();
        }
    }

    private void MaintainReloadDistance()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (pathUpdateTimer < pathUpdateInterval) return;

        if (distance < visionRange - 0.5f)
        {
            Vector3 retreatPos = FindRetreatPosition(player.position, visionRange);
            if (retreatPos != Vector3.zero)
            {
                agent.SetDestination(retreatPos);
                pathUpdateTimer = 0f;
            }
        }
        else if (distance > visionRange + 0.5f)
        {
            agent.SetDestination(player.position);
            pathUpdateTimer = 0f;
        }
        else
        {
            agent.ResetPath();
        }
    }

    private Vector3 FindRetreatPosition(Vector3 fromPosition, float desiredDistance)
    {
        Vector3 directionAway = (transform.position - fromPosition).normalized;
        Vector3 target = fromPosition + directionAway * desiredDistance;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            return hit.position;

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 offsetDir = new(
                directionAway.x * Mathf.Cos(angle) - directionAway.z * Mathf.Sin(angle),
                0f,
                directionAway.x * Mathf.Sin(angle) + directionAway.z * Mathf.Cos(angle)
            );

            Vector3 candidate = fromPosition + offsetDir.normalized * desiredDistance;
            if (NavMesh.SamplePosition(candidate, out hit, 1.5f, NavMesh.AllAreas))
                return hit.position;
        }

        return transform.position;
    }

    private Vector3 GuessPlayerPosition(float dist)
    {
        Vector3 randomDir = Random.insideUnitSphere * dist;
        randomDir.y = 0f;
        randomDir += player.position;

        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, dist, NavMesh.AllAreas))
            return hit.position;

        return transform.position;
    }

    private void FlipSpriteBasedOnMovement()
    {
        Vector3 velocity = agent.velocity;
        if (Mathf.Abs(velocity.x) > 0.1f)
        {
            Vector3 scale = transform.localScale;
            scale.x = velocity.x < 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void FacePlayer()
    {
        float xDir = player.position.x - transform.position.x;
        if (Mathf.Abs(xDir) > 0.1f)
        {
            Vector3 scale = transform.localScale;
            scale.x = xDir < 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
}
