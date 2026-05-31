using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CharacterInfoUI : MonoBehaviour
{
    [Header("Portrait & HP Bar")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image healthBarFill;

    [Header("Stat Labels")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI atkText;
    [SerializeField] private TextMeshProUGUI defText;
    [SerializeField] private TextMeshProUGUI spdText;
    [SerializeField] private TextMeshProUGUI rngText;

    [Header("Action Buttons (player-controlled unit only)")]
    [SerializeField] private GameObject actionSection;
    [SerializeField] private Button moveButton;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button endTurnButton;

    public event Action<string> OnActionSelected;

    private CharacterObject boundCharacter;

    void Awake()
    {
        moveButton.onClick.AddListener(() => OnActionSelected?.Invoke("Move"));
        attackButton.onClick.AddListener(() => OnActionSelected?.Invoke("Attack"));
        endTurnButton.onClick.AddListener(() => OnActionSelected?.Invoke("EndTurn"));

        gameObject.SetActive(false);
    }

    public void Show(CharacterObject character, bool isControllable)
    {
        Unbind();

        boundCharacter = character;

        boundCharacter.OnHPChanged += HandleHPChanged;

        if (portraitImage != null)
        {
            portraitImage.sprite = character.Portrait;
            portraitImage.enabled = (character.Portrait != null);
        }

        if (nameText != null) nameText.text = character.Name;
        if (atkText  != null) atkText.text  = $"ATK : {character.Damage}";
        if (defText  != null) defText.text  = $"DEF    : {character.Defense}";
        if (spdText  != null) spdText.text  = $"SPD : {character.Speed}";
        if (rngText  != null) rngText.text  = $"RANGE : {character.AtkRange}";

        RefreshHP(character.HP, character.MaxHp);

        if (actionSection != null)
            actionSection.SetActive(isControllable);

        gameObject.SetActive(true);
    }

    public void ShowActionButtons()
    {
        if (actionSection != null)
            actionSection.SetActive(true);
    }

    public void HideActionButtons()
    {
        if (actionSection != null)
            actionSection.SetActive(false);
    }

    public void Hide()
    {
        Unbind();
        gameObject.SetActive(false);
    }

    // PRIVATE HELPERS

    private void HandleHPChanged(int currentHP, int maxHP)
    {
        RefreshHP(currentHP, maxHP);
    }

    private void RefreshHP(int currentHP, int maxHP)
    {
        float fraction = maxHP > 0 ? (float)currentHP / maxHP : 0f;

        if (healthBarFill != null)
            healthBarFill.fillAmount = fraction;

        if (hpText != null)
            hpText.text = $"HP  : {currentHP} / {maxHP}";
    }

    private void Unbind()
    {
        if (boundCharacter != null)
        {
            boundCharacter.OnHPChanged -= HandleHPChanged;
            boundCharacter = null;
        }
    }

    void OnDestroy()
    {
        Unbind();
    }
}
