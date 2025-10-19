using UnityEngine;
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP;

    private PlayerMovement movement;
    [SerializeField] private float invincibilityDuration = 0.4f;
    [SerializeField] private Volume deathVolume;
    private Vignette vignette;

    void Start()
    {
        deathVolume.profile.TryGet(out vignette);
    }

    void Awake()
    {
        currentHP = maxHP;
        movement = GetComponent<PlayerMovement>();

        // HealthChanged?.Invoke(currentHP, maxHP);
    }

    

    

}
