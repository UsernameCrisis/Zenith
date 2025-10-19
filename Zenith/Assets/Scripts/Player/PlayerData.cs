using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;
using UnityEngine.Rendering;

public class PlayerData : Character
{
    private Vignette vignette;
    private int maxHP;
    private bool isInvincible = false;
    public event Action<int, int> HealthChanged;
    private Inventory inventory;
    private float invincibilityDuration;
    public void Initialize(int baseHP, int baseSPD, Volume deathVolume, Inventory inventory, float invincibilityDuration)
    {
        deathVolume.profile.TryGet(out vignette);
        HP = baseHP;
        speed = baseSPD;
        maxHP = HP;
        inventory = this.inventory;
        invincibilityDuration = this.invincibilityDuration;
    }

    public void TakeDamage(int amount)
    {
        if (HP <= 0 || isInvincible) return;

        HP -= amount;
        HP = Mathf.Clamp(HP, 0, maxHP);
        HealthChanged?.Invoke(HP, maxHP);
    }

    public int getHP() { return HP; }
    public Vignette getVignette() { return vignette; }
}
