using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ArmorPotion_H", menuName = "Scriptable Objects/ArmorPotion_H")]
public class H_ArmorPotion : ItemBaseScript
{
    public override void OnConsume()
    {
        FindAnyObjectByType<PlayerOverworldAttributes>().BuffArmor(35, 300);
        base.OnConsume();
    }
}
