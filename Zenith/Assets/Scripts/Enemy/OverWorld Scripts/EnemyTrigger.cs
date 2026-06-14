using UnityEngine;
using System.Collections.Generic;

public class EnemyTrigger : MonoBehaviour
{
    [Header("Detection Settings")]
    public float checkRadius = 5f;
    public LayerMask enemyLayer;
    public int maxNearbyEnemies = 3;

    private Collider[] hitResults;
    private bool hasTriggered = false;

    private void Awake()
    {
        hitResults = new Collider[maxNearbyEnemies];
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            StartCombatTransition();
        }
    }

    private void StartCombatTransition()
    {
        hasTriggered = true;

        int count = Physics.OverlapSphereNonAlloc(transform.position, checkRadius, hitResults, enemyLayer);

        List<GameObject> uniqueEnemies = new List<GameObject>();
        List<string> encounteredEnemyNames = new List<string>();

        for (int i = 0; i < count; i++)
        {
            GameObject hitObj = hitResults[i].gameObject;
            GameObject enemyMainBody = null;

            if (hitObj.CompareTag("Enemy"))
            {
                enemyMainBody = hitObj;
            }
            else if (hitObj.transform.parent != null && hitObj.transform.parent.CompareTag("Enemy"))
            {
                enemyMainBody = hitObj.transform.parent.gameObject;
            }

            if (enemyMainBody != null)
            {
                if (!uniqueEnemies.Contains(enemyMainBody))
                {
                    uniqueEnemies.Add(enemyMainBody);
                    encounteredEnemyNames.Add(enemyMainBody.name);
                }
            }
        }

        if (encounteredEnemyNames.Count == 0)
        {
            encounteredEnemyNames.Add(transform.parent.gameObject.name);
        }

        GameManager.Instance.PrepareCombat(encounteredEnemyNames, uniqueEnemies,this);
        GameManager.Instance.StartCombatScene();

        // --- TEST BLOCK ---
        //Debug.Log($"Testing Cleanup for: {string.Join(", ", encounteredEnemyNames)}");
        //CleanupDefeatedEnemies(encounteredEnemyNames);
        // ------------------
    }

    public void CleanupDefeatedEnemies(List<string> defeatedNames)
    {
        List<GameObject> enemyObjects = GameManager.Instance.encounterEnemyObjects;
        // int count = Physics.OverlapSphereNonAlloc(transform.position, checkRadius, hitResults, enemyLayer);

        // for (int i = 0; i < count; i++)
        // {
        //     GameObject hitObj = hitResults[i].gameObject;
        //     GameObject enemyMainBody = null;

        //     if (hitObj.CompareTag("Enemy"))
        //     {
        //         enemyMainBody = hitObj;
        //     }
        //     else if (hitObj.transform.parent != null && hitObj.transform.parent.CompareTag("Enemy"))
        //     {
        //         enemyMainBody = hitObj.transform.parent.gameObject;
        //     }

        //     if (enemyMainBody != null && defeatedNames.Contains(enemyMainBody.name))
        //     {
        //         if (transform.parent != null && enemyMainBody == transform.parent.gameObject)
        //         {
        //             continue;
        //         }

        //         Debug.Log($"Cleaning up nearby ally: {enemyMainBody.name}");
        //         Destroy(enemyMainBody);
        //     }
        // }

        foreach (GameObject enemyGO in enemyObjects)
        {
            if (enemyGO == null) continue;

            if (defeatedNames.Contains(enemyGO.name))
            {
                Debug.Log($"[EnemyTrigger] Cleaning up defeated enemy: {enemyGO.name}");
                Destroy(enemyGO);
            }
        }

        if (transform.parent != null && transform.parent.CompareTag("Enemy"))
        {
            Debug.Log($"Cleaning up main target parent: {transform.parent.name}");
            Destroy(transform.parent.gameObject);
        }
        else
        {
            Debug.Log($"Cleaning up main target self: {gameObject.name}");
            Destroy(gameObject);
        }

        GameManager.Instance.encounterEnemyObjects.Clear();
    }
}