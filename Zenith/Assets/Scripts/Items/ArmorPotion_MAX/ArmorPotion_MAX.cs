using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ArmorPotion_MAX", menuName = "Scriptable Objects/ArmorPotion_MAX")]
public class MAX_ArmorPotion : ItemBaseScript
{
    public override void OnConsume()
    {
        FindAnyObjectByType<PlayerOverworldAttributes>().BuffArmor(100, 300);
        base.OnConsume();
    }
}
