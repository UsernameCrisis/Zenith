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

        List<string> encounteredEnemyNames = new();

        for (int i = 0; i < count; i++)
        {
            // Use transform.root as per your previous script to ensure we get the main enemy object
            GameObject enemyObj = hitResults[i].transform.root.gameObject;

            // Ensure we don't add the same enemy twice (if they have multiple colliders)
            if (!encounteredEnemyNames.Contains(enemyObj.name))
            {
                encounteredEnemyNames.Add(enemyObj.name);
            }
        }

        // Send the data to your GameManager
        // We pass 'this' so the GameManager knows who to talk to when combat ends
        GameManager.Instance.PrepareCombat(encounteredEnemyNames, this);
        GameManager.Instance.StartCombatScene();
    }

    public void CleanupDefeatedEnemies(List<string> defeatedNames)
    {
        // One last check to find and destroy the specific objects
        int count = Physics.OverlapSphereNonAlloc(transform.position, checkRadius, hitResults, enemyLayer);

        for (int i = 0; i < count; i++)
        {
            GameObject enemyObj = hitResults[i].transform.root.gameObject;
            if (defeatedNames.Contains(enemyObj.name))
            {
                Destroy(enemyObj);
            }
        }

        // Self-destruct this trigger so the fight can't happen again
        Destroy(gameObject);
    }
}