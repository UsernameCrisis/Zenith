using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class FloatingEye: MonoBehaviour
{
    private enum EnemyState { Idle, Alert, Attack, Reload }

    [Header("Idle Behavior")]
    [SerializeField] float desRadius = 5f;
    [SerializeField] float minDistToDest = 1f;
    [SerializeField] float minWait = 1f;
    [SerializeField] float maxWait = 2f;
    [SerializeField] private float maxMoveDuration = 5f;

    [Header("Alert Behavior")]
    [SerializeField] float visionRange = 7f;
    [SerializeField] private float aggroDuration = 10f;

    [Header("Combat")]
    [SerializeField] int maxAmmo = 5;
    [SerializeField] float attackCooldown = 1.10f;
    [SerializeField] float attackRange = 6f;
    [SerializeField] Transform player;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] NavMeshAgent agent;

    private EnemyState currentState;
    private Vector3 destination;
    private int ammo;
    private bool seePlayer;
    private float attackCooldownTimer;
    private float originalSpeed;

    private float idleCheckTimer = 0f;
    private readonly float idleCheckInterval = 0.5f;
    private float idleWaitTimer = 0f;
    private float idleWaitDuration = 0f;
    private float moveTimeoutTimer = 0f;
    private bool isWaiting = false;

    private float alertCheckTimer = 0f;
    private readonly float alertCheckInterval = 0.2f;
    private float aggroTimer = 0f;
    private Vector3 lastKnownPlayerPosition;
    private float LookAroundTimer = 0f;
    private float LookAroundCooldown = 2f;

    private bool isReloading = false;

    private void Start()
    {
        ammo = maxAmmo;
        originalSpeed = agent.speed;
        PickRandomDestination();
        ChangeState(EnemyState.Idle);
    }

    private void Update()
    {
        // lock rotation
        transform.rotation = Quaternion.identity;

        if (currentState == EnemyState.Attack)
            FacePlayer();
        else
            FlipSpriteBasedOnMovement();

        switch (currentState)
        {
            case EnemyState.Idle: Idle(); break;
            case EnemyState.Alert: Alert(); break;
            case EnemyState.Reload: Reload(); break;
            case EnemyState.Attack: Attack(); break;
        }
    }

    private void ChangeState(EnemyState newState)
    {
        currentState = newState;

        if (newState == EnemyState.Attack || newState == EnemyState.Reload)
        {
            agent.speed = originalSpeed * 0.7f;
        }
        else
        {
            agent.speed = originalSpeed;
        }
        int stateInt = 1;
        switch (newState)
        {
            case EnemyState.Idle: stateInt = 1; break;
            case EnemyState.Alert: stateInt = 2; break;
            case EnemyState.Attack: stateInt = 3; break;
            case EnemyState.Reload: stateInt = 4; break;
        }

        animator.SetInteger("state", stateInt);
    }

    public void FireProjectile()
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
            Debug.Log("See player, moving to alert");
            ChangeState(EnemyState.Alert);
            return;
        }
        // move timeout
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

        //start wait timer (destination reached)
        if (!isWaiting && agent.remainingDistance <= minDistToDest && !agent.pathPending)
        {
            isWaiting = true;
            idleWaitDuration = Random.Range(minWait, maxWait);
            idleWaitTimer = 0f;
        }
        //wait timer
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
        Debug.Log(aggroTimer);
        if (alertCheckTimer >= alertCheckInterval)
        {
            alertCheckTimer = 0f;
            SeePlayerCheck();
        }

        if (seePlayer)
        {
            aggroTimer = 0f;
            if (Vector3.Distance(transform.position, player.position) <= attackRange)
            {
                ChangeState(EnemyState.Attack);
                return;
            }
            agent.SetDestination(player.position);
            return;
        }

        LookAroundTimer += Time.deltaTime;
        if (LookAroundTimer >= LookAroundCooldown)
        {
            LookAroundTimer = 0f;
            StartCoroutine(LookAroundRoutine());
        }
        // fallback
        if (!agent.hasPath && !seePlayer)
        {
            lastKnownPlayerPosition = GuessPlayerPosition(3f);
            agent.SetDestination(lastKnownPlayerPosition);
        }
        aggroTimer += Time.deltaTime;
        if (aggroTimer >= aggroDuration)
        {
            Debug.Log("Player not found, changing to idle");
            ChangeState(EnemyState.Idle);
        }
    }

    private void Attack()
    {
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
        MaintainReloadDistance();
        if (!isReloading)
        {
            isReloading = true;
            StartCoroutine(ReloadRoutine());
        }
    }

    private IEnumerator ReloadRoutine()
    {
        yield return new WaitForSeconds(1.45f);
        ammo = maxAmmo;
        isReloading = false;
        ChangeState(EnemyState.Alert);
    }

    private void PickRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * desRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, desRadius, NavMesh.AllAreas))
        {
            destination = hit.position;
            agent.SetDestination(destination);

            Debug.Log("New destination picked: " + destination);
        }
        else
        {
            Debug.LogWarning("Failed to find valid NavMesh destination.");
        }
    }

    private void SeePlayerCheck()
    {
        player = GameManager.Instance.Player.transform;
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float fovAngle = 95f;

        Vector3 origin = transform.position;
        Vector3 facingDirection = transform.localScale.x < 0 ? Vector3.left : Vector3.right;

        float angleToPlayer = Vector3.Angle(facingDirection, directionToPlayer);
        if (angleToPlayer > fovAngle)
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

        lastKnownPlayerPosition = GuessPlayerPosition(3f);
        agent.SetDestination(lastKnownPlayerPosition);
        LookAroundTimer = 0;
    }

    private void MaintainAttackDistance()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer < attackRange - 0.5f)
        {
            Vector3 retreatPosition = FindRetreatPosition(player.position, attackRange);
            if (retreatPosition != Vector3.zero)
            {
                agent.SetDestination(retreatPosition);
            }
        }
        else
        {
            agent.ResetPath();
        }
    }

    private void MaintainReloadDistance()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer < visionRange - 0.5f)
        {
            Vector3 retreatPosition = FindRetreatPosition(player.position, visionRange);
            if (retreatPosition != Vector3.zero)
            {
                agent.SetDestination(retreatPosition);
            }
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

        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 2f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 offsetDir = new Vector3(
                directionAway.x * Mathf.Cos(angle) - directionAway.z * Mathf.Sin(angle),
                0f,
                directionAway.x * Mathf.Sin(angle) + directionAway.z * Mathf.Cos(angle)
            );
            Vector3 candidate = fromPosition + offsetDir.normalized * desiredDistance;

            if (NavMesh.SamplePosition(candidate, out hit, 1.5f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return transform.position;
    }


    private Vector3 GuessPlayerPosition(float dist)
    {
        Vector3 randomDirection = Random.insideUnitSphere * dist;
        randomDirection.y = 0f;
        randomDirection += player.position;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, dist, NavMesh.AllAreas))
        {
            return hit.position;
        }
        else
        {
            return transform.position;
        }
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
        float xDirection = player.position.x - transform.position.x;
        if (Mathf.Abs(xDirection) > 0.1f)
        {
            Vector3 scale = transform.localScale;
            scale.x = xDirection < 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
}
