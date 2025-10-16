using UnityEngine;

public class AnimationRelay : MonoBehaviour
{
    public EnemyAI enemyAI;

    public void FireProjectile()
    {
        if (enemyAI != null)
            enemyAI.FireProjectile();
    }
}
