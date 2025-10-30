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
