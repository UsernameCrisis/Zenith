using UnityEngine;

public class Floating_Eye : Enemy
{
    void Awake()
    {
        enemyName = "Floating Eye";
        HP = 10;
        speed = 3;
        moves.Add(new Move("Eye Beam", 10));
        nextMove = moves[0];
    }
    public override Move bestMove()
    {
        throw new System.NotImplementedException();
    }

    public override Character bestTarget()
    {
        throw new System.NotImplementedException();
    }

    protected override void die()
    {
        throw new System.NotImplementedException();
    }
}
