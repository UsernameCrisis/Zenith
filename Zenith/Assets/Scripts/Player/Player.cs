using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class Player : Character
{
    [SerializeField] private PlayerMovement playerMovement;
    private List<Character> companions;
    private List<Move> moves = new List<Move>();
    void Start()
    {
        HP = 100;
        speed = 5;
        companions = new List<Character>();

        moves.Add(new Move("Basic Attack", 10));
    }

    public List<Character> GetCompanions()
    {
        return companions;
    }


    // Update is called once per frame
    void Update()
    {

    }

    public void canMove(bool canMove)
    {
        playerMovement.canMove(canMove);
    }

    protected override void die()
    {
        throw new NotImplementedException();
    }

    //temp
    public List<Move> getMoves()
    {
        return moves;
    }
}
