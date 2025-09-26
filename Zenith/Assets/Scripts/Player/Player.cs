using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : Character
{
    [SerializeField] private PlayerMovement playerMovement;
    private List<Character> companions;
    void Start()
    {
        HP = 100;
        speed = 5;
        companions = new List<Character>();
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
    public float getSpeed()
    {
        return speed;
    }

    protected override void die()
    {
        throw new NotImplementedException();
    }
}
