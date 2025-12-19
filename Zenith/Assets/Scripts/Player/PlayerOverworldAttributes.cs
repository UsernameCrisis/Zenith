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
    public Inventory inventory;

    [Header("Screen Fade")]
    [SerializeField] private Volume deathVolume;
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float vignetteDuration = 1f;
    [SerializeField] private float postVignetteWait = 1f;
    [SerializeField] private float fadeToBlackDuration = 2f;


    public event Action<int, int> HealthChanged;

    void Start()
    {
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

            GameManager.Instance.UpdateStats();
        }

        if (currentHP == 0)
        {
            currentHP++;
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

    IEnumerator DeathSequence(float vignetteDuration = 1f)
    {
        float elapsed = 0f;
        float startIntensity = 0.3f;
        float targetIntensity = 0.75f;

        while (elapsed < vignetteDuration)
        {
            float t = elapsed / vignetteDuration;
            vignette.intensity.value = Mathf.Lerp(startIntensity, targetIntensity, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        vignette.intensity.value = targetIntensity;

        yield return new WaitForSecondsRealtime(postVignetteWait);
        yield return Fade(1f, fadeToBlackDuration);

        DeathReset();
        Time.timeScale = 1f;
        SceneManager.LoadScene("Peaceful");
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

        if (Mathf.Approximately(targetAlpha, 0f))
        {
            fadeOverlay.blocksRaycasts = false;
            fadeOverlay.interactable = false;
        }
    }

    public void DeathReset()
    {
        currentHP = maxHP;
        SaveAttributesToManager();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearInventoryData();
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
}
