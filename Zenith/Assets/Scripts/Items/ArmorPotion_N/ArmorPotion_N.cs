using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ArmorPotion_N", menuName = "Scriptable Objects/ArmorPotion_N")]
public class N_ArmorPotion : ItemBaseScript
{
    public override void OnConsume()
    {
        FindAnyObjectByType<PlayerOverworldAttributes>().BuffArmor(5, 300);
        base.OnConsume();
    }
}
