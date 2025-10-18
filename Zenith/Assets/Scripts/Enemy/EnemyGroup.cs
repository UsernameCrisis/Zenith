using System.Collections.Generic;
using UnityEngine;

public class EnemyGroup : MonoBehaviour
{
    [SerializeField] private Enemy enemy1;
    [SerializeField] private Enemy enemy2;
    [SerializeField] private Enemy enemy3;
    private List<Enemy> enemies;
    void Awake()
    {
        enemies.Add(enemy1);
        enemies.Add(enemy2);
        enemies.Add(enemy3);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public List<Enemy> getEnemies()
    {
        return enemies;
    }
}
