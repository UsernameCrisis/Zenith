using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ArmorPotion_L", menuName = "Scriptable Objects/ArmorPotion_L")]
public class L_ArmorPotion : ItemBaseScript
{
    public override void OnConsume()
    {
        FindAnyObjectByType<PlayerOverworldAttributes>().BuffArmor(15, 300);
        base.OnConsume();
    }
}
