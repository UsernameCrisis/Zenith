using UnityEngine;
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class PlayerOverworldAttributes : MonoBehaviour
{
    public int maxHP;
    public int currentHP;
    public int gold;

    private PlayerMovement movement;
    private bool isInvincible = false;
    [SerializeField] private float invincibilityDuration = 0.4f;
    [SerializeField] private Volume deathVolume;
    private Vignette vignette;

    public event Action<int, int> HealthChanged;

    void Start()
    {
        deathVolume.profile.TryGet(out vignette);
    }

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        currentHP = GameManager.Instance.playerHP;
        maxHP = GameManager.Instance.playerMaxHP;
        gold = GameManager.Instance.gold;
        HealthChanged?.Invoke(currentHP, maxHP);
    }

    public void TakeDamage(int amount)
    {
        if (currentHP <= 0 || isInvincible) return;

        currentHP -= amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        HealthChanged?.Invoke(currentHP, maxHP);

        if (currentHP > 0)
        {
            movement.TakeHit();
            StartCoroutine(InvincibilityFrames());
        }
        else
        {
            movement.Die();
            StartCoroutine(DeathVignette());
        }
    }

    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    IEnumerator DeathVignette(float duration = 1f)
    {
        float elapsed = 0f;
        float startIntensity = 0.3f;
        float targetIntensity = 0.75f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            vignette.intensity.value = Mathf.Lerp(startIntensity, targetIntensity, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        vignette.intensity.value = targetIntensity;
    }

}
