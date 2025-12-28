using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HealthPotion_MAX", menuName = "Scriptable Objects/HealthPotion_MAX")]
public class MAX_HealthPotion : ItemBaseScript
{
    public override void OnConsume()
    {
        FindAnyObjectByType<PlayerOverworldAttributes>().currentHP = Math.Min(FindAnyObjectByType<PlayerOverworldAttributes>().currentHP + 999, FindAnyObjectByType<PlayerOverworldAttributes>().maxHP);
        FindAnyObjectByType<OverworldUI>().UpdateUI(
            FindAnyObjectByType<PlayerOverworldAttributes>().currentHP,
            FindAnyObjectByType<PlayerOverworldAttributes>().maxHP
        );
        base.OnConsume();
    }
}
