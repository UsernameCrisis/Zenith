using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject[] enemyPrefabs;

    [Range(0f, 1f)]
    public float spawnProbability = 0.5f;

    void Start()
    {
        TrySpawn();
    }

    public void TrySpawn()
    {
        float roll = Random.value;

        if (roll <= spawnProbability)
        {
            int randomIndex = Random.Range(0, enemyPrefabs.Length);
            Instantiate(enemyPrefabs[randomIndex], transform.position, Quaternion.identity);
        }
    }
}