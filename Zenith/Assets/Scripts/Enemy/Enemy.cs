using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public abstract class Enemy : Character
{
    protected string enemyName;
    protected Move nextMove;
    protected List<Move> moves = new List<Move>();
    private float atb_max = 100;
    private float atb_current;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        atb_current = 0;
    }

    // Update is called once per frame
    void Update()
    {
    }
    public abstract Move bestMove();
    public abstract Character bestTarget();

    public void ATBAdd(float amount)
    {
        if (atb_current < atb_max)
        {
            atb_current += amount;
        }
    }

    public void resetATB()
    {
        atb_current = 0;
    }

    protected override void die()
    {
    }

    public float getATB()
    {
        return atb_current;
    }

}
