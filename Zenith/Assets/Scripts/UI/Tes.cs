using UnityEngine;

public class Tes : MonoBehaviour
{
    [SerializeField] private Transform transform;
    void Start()
    {
        gameObject.SetActive(true);
    }
    public void DoDamage()
    {
        int damage = Random.Range(100, 1500);

        SpawnsDamagePopups.Instance.DamageDone(damage, transform.position, false);
    }
}
