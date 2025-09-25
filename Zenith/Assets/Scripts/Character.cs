using System;
using UnityEngine;

public abstract class Character : MonoBehaviour
{
    protected int HP;
    protected int speed;

    internal int GetHP()
    {
        return HP;
    }
}
