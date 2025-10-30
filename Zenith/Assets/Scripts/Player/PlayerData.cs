using UnityEngine;
using System;
using System.Collections;

public class PlayerData : MonoBehaviour
{
    [SerializeField] private int maxHP = 100;
    private int currentHP;
    private bool isInvincible = false;
    [SerializeField] private float invincibilityDuration = 0.4f;
    public bool isDead { get; set; }

    public event Action<int, int> HealthChanged;


    void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int amount)
    {
        if (currentHP <= 0 || isInvincible) return;

        currentHP -= amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        HealthChanged?.Invoke(currentHP, maxHP);

        if (currentHP > 0)
        {
            GameManager.Instance.Player.GetComponent<PlayerEventManager>().TakeHit();
        }
        else
        {
            StartCoroutine(GameManager.Instance.Player.GetComponent<PlayerEventManager>().Die());
        }

    }
    
    public int getMaxHP() { return maxHP; }
    public int getHP() { return currentHP; }
    public void resetHP() { currentHP = maxHP; MainCanvasManager.Instance.PlayerHealthUI.UpdateUI(); }
}
