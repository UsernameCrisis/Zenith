using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class PlayerOverworldAttributes : MonoBehaviour
{
    [Header("Default Values")]
    public int maxHP;
    public int currentHP;
    public int gold;

    private PlayerMovement movement;
    private bool isInvincible = false;
    [SerializeField] private float invincibilityDuration = 0.4f;
    private Vignette vignette;

    [Header("Screen Fade")]
    [SerializeField] private Volume deathVolume;
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float vignetteDuration = 1f;
    [SerializeField] private float postVignetteWait = 1f;
    [SerializeField] private float fadeToBlackDuration = 2f;

    public event Action<int, int> HealthChanged;

    void Start()
    {
        if (deathVolume != null)
            deathVolume.profile.TryGet(out vignette);

        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
            fadeOverlay.interactable = false;
        }
    }

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        if (GameManager.Instance != null && !GameManager.Instance.hasData)
        {
            currentHP = GameManager.Instance.playerHP;
            maxHP = GameManager.Instance.playerMaxHP;
            gold = GameManager.Instance.gold;
        }

        if (currentHP <= 0)
        {
            currentHP = 1;
        }
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
            StartCoroutine(DeathSequence());
        }
        SaveAttributesToManager();
    }

    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    IEnumerator DeathSequence()
    {
        float elapsed = 0f;
        float startIntensity = 0.3f;
        float targetIntensity = 0.75f;

        while (elapsed < vignetteDuration)
        {
            float t = elapsed / vignetteDuration;
            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(startIntensity, targetIntensity, t);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (vignette != null) vignette.intensity.value = targetIntensity;

        yield return new WaitForSecondsRealtime(postVignetteWait);
        yield return Fade(1f, fadeToBlackDuration);

        DeathReset();
        Time.timeScale = 1f;
        LoadingScreenManager.Load("Peaceful");
    }

    private IEnumerator Fade(float targetAlpha, float duration)
    {
        if (fadeOverlay == null) yield break;

        float startAlpha = fadeOverlay.alpha;
        float time = 0f;
        bool goingDark = targetAlpha > startAlpha;

        fadeOverlay.blocksRaycasts = goingDark;
        fadeOverlay.interactable = goingDark;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / duration;
            fadeOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        fadeOverlay.alpha = targetAlpha;
    }

    public void DeathReset()
    {
        currentHP = maxHP;
        SaveAttributesToManager();

        // --- TRIGGER DEATH PENALTY ---
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.HandleDeathPenalty();
        }
    }

    public void SaveAttributesToManager()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerHP = currentHP;
            GameManager.Instance.playerMaxHP = maxHP;
            GameManager.Instance.gold = gold;
        }
    }

    public void Heal(int amount)
    {
        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        HealthChanged?.Invoke(currentHP, maxHP);
        SaveAttributesToManager();
    }

    public void AddGold(int amount)
    {
        gold += amount;
        SaveAttributesToManager();
    }

    // --- BUFF LOGIC ---
    public void BuffArmor(int amount, int seconds)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.playerDef += amount;
        StartCoroutine(ResetArmorBuff(amount, seconds));
    }

    private IEnumerator ResetArmorBuff(int amount, int seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (GameManager.Instance != null) GameManager.Instance.playerDef -= amount;
    }

    public void BuffATK(int amount, int seconds)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.playerAtk += amount;
        StartCoroutine(ResetATKBuff(amount, seconds));
    }

    private IEnumerator ResetATKBuff(int amount, int seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (GameManager.Instance != null) GameManager.Instance.playerAtk -= amount;
    }

    public void RefreshHealthUI()
    {
        HealthChanged?.Invoke(currentHP, maxHP);
    }
}