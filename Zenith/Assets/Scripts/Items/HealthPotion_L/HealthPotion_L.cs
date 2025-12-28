using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HealthPotion_L", menuName = "Scriptable Objects/HealthPotion_L")]
public class L_HealthPotion : ItemBaseScript
{
    public override void OnConsume()
    {
        FindAnyObjectByType<PlayerOverworldAttributes>().currentHP = Math.Min(FindAnyObjectByType<PlayerOverworldAttributes>().currentHP + 35, FindAnyObjectByType<PlayerOverworldAttributes>().maxHP);
        FindAnyObjectByType<OverworldUI>().UpdateUI(
            FindAnyObjectByType<PlayerOverworldAttributes>().currentHP,
            FindAnyObjectByType<PlayerOverworldAttributes>().maxHP
        );
        base.OnConsume();
    }
}
