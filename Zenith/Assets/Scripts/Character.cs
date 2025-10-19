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
    public void takeDamage(int damage)
    {
        HP -= damage;

        StartCoroutine(removeDamageNumber());
    }

    protected IEnumerator removeDamageNumber()
    {
        yield return new WaitForSeconds((float)0.5);
    }

    public float getSpeed(){return speed;}
    
    public int getHP(){return HP;}
}
