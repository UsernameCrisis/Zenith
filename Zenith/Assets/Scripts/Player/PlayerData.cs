using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

public class PlayerData : Character
{
    [SerializeField] private int baseHP;
    [SerializeField] private int baseSPD;
    [SerializeField] private float invincibilityDuration = 0.4f;
    [SerializeField] private Volume deathVolume;
    [SerializeField] private Inventory inventory;

    private Vignette vignette;
    private int maxHP;
    private bool isInvincible = false;
    public event Action<int, int> HealthChanged;
    public void Initialize(int baseHP, int baseSPD)
    {
        deathVolume.profile.TryGet(out vignette);
        HP = baseHP;
        speed = baseSPD;
        maxHP = HP;
    }

    public void TakeDamage(int amount)
    {
        if (HP <= 0 || isInvincible) return;

        HP -= amount;
        HP = Mathf.Clamp(HP, 0, maxHP);
        HealthChanged?.Invoke(HP, maxHP);
    }
    
    public int getHP()
    {
        return HP;
    }
}
