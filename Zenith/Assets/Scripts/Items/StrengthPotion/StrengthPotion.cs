using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "StrengthPotion", menuName = "Scriptable Objects/StrengthPotion")]
public class StrengthPotion : ItemBaseScript
{
    public override void OnConsume()
    {
        FindAnyObjectByType<PlayerOverworldAttributes>().BuffATK(10, 300);

        base.OnConsume();
    }
}
