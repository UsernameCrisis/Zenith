using UnityEngine;

public abstract class Projectile : MonoBehaviour
{
    [SerializeField] protected float speed;
    [SerializeField] protected int attackPower;
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

    public void IgnoreShooter(Collider shooter)
    {
        shooterCollider = shooter;
        Collider myCollider = GetComponent<Collider>();
        if (myCollider != null && shooter != null)
        {
            Physics.IgnoreCollision(myCollider, shooter);
        }
    }

    public int getDamage()
    {
        return attackPower;
    }
}
