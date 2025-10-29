using System;
using System.Collections;
using TMPro;
using UnityEngine;

public abstract class Character : MonoBehaviour
{
    protected int HP;
    protected float speed;

    void Awake()
    {
    }
    internal int GetHP()
    {
        return HP;
    }
    public void takeDamage(int damage)
    {
        HP -= damage;


        StartCoroutine(removeDamageNumber());

        if (HP < 0) die();
    }

    protected IEnumerator removeDamageNumber()
    {
        yield return new WaitForSeconds((float)0.5);
    }

    protected abstract void die();

    public float getSpeed()
    {
        return speed;
    }
    
}
