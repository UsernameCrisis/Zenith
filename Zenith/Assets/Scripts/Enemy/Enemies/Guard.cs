using Unity.VisualScripting;
using UnityEngine;

public class Guard : Enemy
{
    void Awake()
    {
        enemyName = "Guard";
        HP = 10;
        speed = 3;
        moves.Add(new Move("Slash", 10));
        nextMove = moves[0];
        
    }
    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {
        //setBestMove();
        //setBestTarget();
    }

    public override Move bestMove()
    {
        //implement logic
        nextMove = moves[0];

        return nextMove;
    }
    public override Character bestTarget()
    {
        //implement logic
        return FindObjectOfType<Player>();
    }

    public Move GetNextMove()
    {
        return nextMove;
    }

    protected override void die()
    {
    }
}
