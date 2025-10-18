using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float speed = 10f;
    private Vector3 direction;
    private Collider shooterCollider;

    public void Initialize(Vector3 targetDirection)
    {
        direction = targetDirection.normalized;
        Destroy(gameObject, 5f);
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == shooterCollider) return;

        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                player.TakeHit();
            }
        }

        Debug.Log(other);
        Destroy(gameObject);
    }

    public void IgnoreShooter(Collider shooter)
    {
        shooterCollider = shooter;
        Collider myCollider = GetComponent<Collider>();
        if (myCollider != null && shooter != null)
        {
            Physics.IgnoreCollision(myCollider, shooter);
        }
    }
}
