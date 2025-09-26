using System;
using System.Collections;
using TMPro;
using UnityEngine;

public abstract class Character : MonoBehaviour
{
    [SerializeField] GameObject damageTakenNumber;
    protected int HP;
    protected float speed;

    void Awake()
    {
        damageTakenNumber.SetActive(false);
        damageTakenNumber.GetComponent<TMP_Text>().SetText("");
    }
    internal int GetHP()
    {
        return HP;
    }
    internal void takeDamage(int damage)
    {
        HP -= damage;
        
        damageTakenNumber.GetComponent<TMP_Text>().SetText(damage.ToString());
        damageTakenNumber.SetActive(true);

        StartCoroutine(removeDamageNumber());

        if (HP < 0) die();
    }

    protected IEnumerator removeDamageNumber()
    {
        yield return new WaitForSeconds((float)0.5);
        damageTakenNumber.SetActive(false);
    }

    protected abstract void die();
}
