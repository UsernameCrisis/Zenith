using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MAX_HealthPotion", menuName = "Scriptable Objects/MAX_HealthPotion")]
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
