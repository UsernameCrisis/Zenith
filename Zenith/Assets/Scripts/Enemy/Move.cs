using UnityEngine;

public class Move {
    private string name;
    private int damage;

    public Move(string name, int damage)
    {
        this.name = name;
        this.damage = damage;
    }


    public string getName()
    {
        return name;
    }

    public int getDamage()
    {
        return damage;
    }
}