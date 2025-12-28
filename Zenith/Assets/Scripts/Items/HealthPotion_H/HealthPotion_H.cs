using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HealthPotion_H", menuName = "Scriptable Objects/HealthPotion_H")]
public class H_HealthPotion : ItemBaseScript
{
    public override void OnConsume()
    {
        FindAnyObjectByType<PlayerOverworldAttributes>().currentHP = Math.Min(FindAnyObjectByType<PlayerOverworldAttributes>().currentHP + 80, FindAnyObjectByType<PlayerOverworldAttributes>().maxHP);
        FindAnyObjectByType<OverworldUI>().UpdateUI(
            FindAnyObjectByType<PlayerOverworldAttributes>().currentHP,
            FindAnyObjectByType<PlayerOverworldAttributes>().maxHP
        );
        base.OnConsume();
    }
}
