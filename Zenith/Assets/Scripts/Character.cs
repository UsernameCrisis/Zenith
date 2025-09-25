using System;
using UnityEngine;

public abstract class Character : MonoBehaviour
{
    protected int HP;
    protected float speed;

    internal int GetHP()
    {
        return HP;
    }
    internal void takeDamage(int damage)
    {
        HP -= damage;
        if (HP < 0) die();
    }

    protected abstract void die();
}
