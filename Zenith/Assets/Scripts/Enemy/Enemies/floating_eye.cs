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
    [SerializeField] float cooldown_attack = 1f;
    [SerializeField] float attackRange = 6f;
    [SerializeField] Transform player;

    [Header("References")]
    [SerializeField] private Animator animator;

    private EnemyState currentState;
    private Vector3 destination;
    private int ammo;
    private bool see_player;
    private float attackCooldownTimer;

    private void Start()
    {
        ammo = maxAmmo;
        ChangeState(EnemyState.Idle);
    }

    private void Update()
    {
        UpdateAnimator();

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
        animator.SetInteger("state", (int)newState);
    }

    private void UpdateAnimator()
    {
        animator.SetBool("see_player", see_player);
        animator.SetInteger("ammo", ammo);
        animator.SetFloat("cooldown_attack", attackCooldownTimer);
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
        if (!see_player)
        {
            ChangeState(EnemyState.Searching);
            return;
        }

        transform.LookAt(player);

        if (attackCooldownTimer > 0)
        {
            attackCooldownTimer -= Time.deltaTime;
            return;
        }

        if (ammo > 0)
        {
            ammo--;
            attackCooldownTimer = cooldown_attack;
        }
        else
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
        yield return new WaitForSeconds(2f);
        ammo = maxAmmo;
        ChangeState(EnemyState.Searching);
    }

    private void MoveToDestination()
    {
        transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
    }

    private void MoveToPlayer()
    {
        transform.position = Vector3.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
    }

    private void PickRandomDestination()
    {
        Vector2 random = Random.insideUnitCircle * desRadius;
        destination = new Vector3(transform.position.x + random.x, transform.position.y, transform.position.z + random.y);
    }

    private void SeePlayerCheck()
    {
        if (Vector3.Distance(transform.position, player.position) < 10f)
        {
            see_player = true;
            ChangeState(EnemyState.Searching);
        }
        else
            see_player = false;
    }
}
