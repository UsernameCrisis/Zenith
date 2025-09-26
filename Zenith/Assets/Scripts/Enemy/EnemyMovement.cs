using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float moveSpeed = 100;

    [Header("Destination")]
    [SerializeField] private float destRadius = 5f;
    [SerializeField] private float minDistToDest = 0.5f;

    [Header("Wait Timer")]
    [SerializeField] private float minWait = 1f;
    [SerializeField] private float maxWait = 3f;
    private EnemyState _currentState;
    private float waitTimer;
    private Vector3 dest;
    private bool hasCollided = false;
    private NavMeshAgent agent;
    enum EnemyState
    {
        Idle,
        Roaming
    }

    private EnemyState CurrentState
    {
        get { return _currentState; }
        set
        {
            if (_currentState == value) return;
            _currentState = value;

            switch (_currentState)
            {
                case EnemyState.Idle:
                    // All variable that is needed for idle state goes here
                    waitTimer = Random.Range(minWait, maxWait);
                    break;
                case EnemyState.Roaming:
                    // All variable that is needed for roaming state goes here
                    dest = DestinationRandomizer(transform.position, destRadius);
                    hasCollided = false;
                    break;
            }
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        CurrentState = EnemyState.Idle;
    }

    void FixedUpdate()
    {
        switch (CurrentState)
        {
            case EnemyState.Idle:
                waitTimer -= Time.fixedDeltaTime;

                if (waitTimer <= 0f)
                {
                    CurrentState = EnemyState.Roaming;
                }
                break;

            case EnemyState.Roaming:

                MoveTowards(dest);

                if (Vector3.Distance(transform.position, dest) < minDistToDest)
                {
                    CurrentState = EnemyState.Idle;
                }
                break;
        }
    }

    private void MoveTowards(Vector3 dest)
    {
        agent.SetDestination(dest);

        // Vector2 dir = (dest - (Vector2)transform.position).normalized;
        // rb.AddForce(new Vector2(dir.x * moveSpeed, dir.y * moveSpeed), ForceMode2D.Force);
    }

    private Vector3 DestinationRandomizer(Vector3 center, float radius)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector3 randomDir = Random.insideUnitSphere * radius;
            Vector3 candidate = center + randomDir;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return center;
    }

    // private void OnCollisionStay(Collision collision)
    // {
    //     if (CurrentState == EnemyState.Roaming && !hasCollided)
    //     {
    //         if (collision.gameObject.CompareTag("Wall"))
    //         {
    //             rb.linearVelocity = Vector2.zero;
    //             CurrentState = EnemyState.Idle;
    //             hasCollided = true;
    //         }
    //     }
    // }
}
