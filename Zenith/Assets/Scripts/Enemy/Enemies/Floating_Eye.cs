using UnityEngine;

public class Floating_Eye : Enemy
{
    public override Move bestMove()
    {
        throw new System.NotImplementedException();
    }

    public override Character bestTarget()
    {
        throw new System.NotImplementedException();
    }

    void Awake()
    {
        enemyName = "Floating Eye";
        HP = 10;
        speed = 3;
        moves.Add(new Move("Eye Beam", 10));
        nextMove = moves[0];
    }
}
