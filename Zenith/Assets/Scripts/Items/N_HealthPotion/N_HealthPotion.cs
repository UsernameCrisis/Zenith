using System;
using UnityEngine;

[CreateAssetMenu(fileName = "N_HealthPotion", menuName = "Scriptable Objects/N_HealthPotion")]
public class N_HealthPotion : ItemBaseScript
{
    public override void OnConsume()
    {
        FindAnyObjectByType<PlayerOverworldAttributes>().currentHP = Math.Min(FindAnyObjectByType<PlayerOverworldAttributes>().currentHP + 15, FindAnyObjectByType<PlayerOverworldAttributes>().maxHP);
        FindAnyObjectByType<OverworldUI>().UpdateUI(
            FindAnyObjectByType<PlayerOverworldAttributes>().currentHP,
            FindAnyObjectByType<PlayerOverworldAttributes>().maxHP
        );
        base.OnConsume();
    }
}
