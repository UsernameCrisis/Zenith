using System.Collections.Generic;
using UnityEngine;

public class EnemyGroup : MonoBehaviour
{
    [SerializeField] private List<Enemy> enemies;
    public List<Enemy> GetEnemies()
    {
        return enemies;
    }
}
