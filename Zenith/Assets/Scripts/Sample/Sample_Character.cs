using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;

public class Sample_Character : SampleEnemy
{
    [SerializeField] private float maxhp;
    [SerializeField] private float hp;

    public void TakeDamage(int damage)
    {
        hp = MathF.Max(0, hp - damage);
    }

    public float GetHPPercentage()
    {
        return hp / maxhp;
    }


}
