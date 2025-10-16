using UnityEngine;
using System.Collections;

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
    private Vector3 lockedTargetPosition;


    private void Start()
    {
        ammo = maxAmmo;
        ChangeState(EnemyState.Idle);
    }

    private void Update()
    {
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

        if (newState == EnemyState.Attack)
        {
            lockedTargetPosition = player.position;
        }

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
        Vector3 direction = (lockedTargetPosition - firePoint.position).normalized;
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
            MoveToDestination();
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
        UpdateFacingDirection(destination);
        transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);

    }

    private void MoveToPlayer()
    {
        UpdateFacingDirection(player.position);
        transform.position = Vector3.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
    }

    private void PickRandomDestination()
    {
        Vector2 random = Random.insideUnitCircle * desRadius;
        destination = new Vector3(transform.position.x + random.x, transform.position.y, transform.position.z + random.y);
    }

    private void SeePlayerCheck()
    {
        if (Vector3.Distance(transform.position, player.position) < 8f)
        {
            see_player = true;
            ChangeState(EnemyState.Searching);
        }
        else
            see_player = false;
    }

    private void UpdateFacingDirection(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        if (direction.x < 0)
            transform.localScale = new Vector3(-1f, transform.localScale.y, transform.localScale.z);
        else if (direction.x > 0)
            transform.localScale = new Vector3(1f, transform.localScale.y, transform.localScale.z);
    }

}
