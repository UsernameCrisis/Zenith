using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    private enum EnemyState { Idle, Searching, Attack, Reload }

    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float desRadius = 8f;
    [SerializeField] float minDistToDest = 0.5f;

    [Header("Idle Behavior")]
    [SerializeField] float minWait = 1f;
    [SerializeField] float maxWait = 3f;

    [Header("Combat")]
    [SerializeField] int maxAmmo = 5;
    [SerializeField] float cooldown_attack = 1.10f;
    [SerializeField] float attackRange = 6f;
    [SerializeField] Transform player;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform firePoint;

    private EnemyState currentState;
    private Vector3 destination;
    private int ammo;
    private bool see_player;
    private float attackCooldownTimer;
    private NavMeshAgent agent;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        ammo = maxAmmo;
        PickRandomDestination();
        ChangeState(EnemyState.Idle);
    }

    private void Update()
    {
        // lock rotation
        transform.rotation = Quaternion.identity;
        Idle();
        switch (currentState)
        {
            case EnemyState.Idle: Idle(); break;
            case EnemyState.Searching: Searching(); break;
            case EnemyState.Attack: Attack(); break;
            case EnemyState.Reload: Reload(); break;
        }
    }

    private void ChangeState(EnemyState newState)
    {
        currentState = newState;

        int stateInt = 1;
        switch (newState)
        {
            case EnemyState.Idle: stateInt = 1; break;
            case EnemyState.Searching: stateInt = 2; break;
            case EnemyState.Attack: stateInt = 3; break;
            case EnemyState.Reload: stateInt = 4; break;
        }

        animator.SetInteger("state", stateInt);
    }

    public void FireProjectile()
    {
        if (currentState != EnemyState.Attack || ammo <= 0) return;

        ammo--;
        attackCooldownTimer = cooldown_attack;

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
        if (Random.value < 0.01f) SeePlayerCheck();

        if (Vector3.Distance(transform.position, destination) < minDistToDest)
        {
            StartCoroutine(WaitThenMove());
        }
        else
        {
            MoveToDestination();
        }
    }

    private IEnumerator WaitThenMove()
    {
        ChangeState(EnemyState.Idle);
        yield return new WaitForSeconds(Random.Range(minWait, maxWait));
        PickRandomDestination();
    }

    private void Searching()
    {
        if (!see_player)
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        MoveToPlayer();

        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            ChangeState(EnemyState.Attack);
        }
    }

    private void Attack()
    {
        attackCooldownTimer -= Time.deltaTime;

        if (attackCooldownTimer <= 0f && ammo > 0)
        {
            FireProjectile();
            attackCooldownTimer = cooldown_attack;
        }
        else if (ammo <= 0)
        {
            ChangeState(EnemyState.Reload);
        }
    }

    private void Reload()
    {
        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        yield return new WaitForSeconds(1.45f);
        ammo = maxAmmo;
        ChangeState(EnemyState.Searching);
    }

    private void MoveToDestination()
    {
        agent.SetDestination(destination);

        // Flip sprite based on movement direction
        Vector3 dir = destination - transform.position;
        if (Mathf.Abs(dir.x) > 0.1f)
            UpdateFacingDirection(dir.x);
    }

    private void MoveToPlayer()
    {
        agent.SetDestination(player.position);

        // Flip sprite based on movement direction
        Vector3 dir = player.position - transform.position;
        if (Mathf.Abs(dir.x) > 0.1f)
            UpdateFacingDirection(dir.x);
    }

    private void PickRandomDestination()
    {
        Vector2 random = Random.insideUnitCircle * desRadius;
        destination = new Vector3(transform.position.x + random.x, transform.position.y, transform.position.z + random.y);
    }

    private void SeePlayerCheck()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float visionRange = 8f;
        float fovAngle = 60f;

        Vector3 origin = transform.position;
        Vector3 facingDirection = transform.localScale.x < 0 ? Vector3.left : Vector3.right;

        float angleToPlayer = Vector3.Angle(facingDirection, directionToPlayer);
        if (angleToPlayer > fovAngle)
        {
            see_player = false;
            return;
        }

        if (Physics.Raycast(origin, directionToPlayer, out RaycastHit hit, visionRange))
        {
            if (hit.transform == player)
            {
                see_player = true;
                ChangeState(EnemyState.Searching);
                return;
            }
        }
        see_player = false;
    }

    private void UpdateFacingDirection(float xDirection)
    {
        if (xDirection < 0)
            transform.localScale = new Vector3(-1f, transform.localScale.y, transform.localScale.z);
        else if (xDirection > 0)
            transform.localScale = new Vector3(1f, transform.localScale.y, transform.localScale.z);
    }
}
