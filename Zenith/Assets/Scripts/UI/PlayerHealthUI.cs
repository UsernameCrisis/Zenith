using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class OverworldUI : MonoBehaviour
{
    [SerializeField] private Slider hpBar;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private GameObject Gold;
    [SerializeField] private PlayerOverworldAttributes playerOverworldAttributes;

    [Header("Visual Feedback")]
    [SerializeField] private RectTransform hpBarTransform;
    [SerializeField] private Image fillImage;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private Color normalColor = Color.red;

    [Header("Inventory System")]
    public InventoryDisplayUI inventoryDisplay;
    public GameObject inventoryObject;

    [Header("Other UI")]
    public GameObject interactUI;
    [SerializeField] private GameObject dialoguePanel;
    // ItemDescriptionObject removed to stop errors

    void Awake()
    {
        if (playerOverworldAttributes != null)
        {
            playerOverworldAttributes.HealthChanged += UpdateUI;
        }
    }

    void Start()
    {
        hpText.enabled = false;
        Gold.SetActive(false);
        UpdateUI(playerOverworldAttributes.currentHP, playerOverworldAttributes.maxHP);

        if (inventoryObject != null) inventoryObject.SetActive(false);
        interactUI.SetActive(false);
    }

    void Update()
    {
        bool isGifting = InventoryManager.Instance != null && InventoryManager.Instance.isGifting;
        bool isShopping = InventoryManager.Instance != null && InventoryManager.Instance.isShopping;

        if ((isGifting || isShopping) && inventoryObject.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || InputSystem.actions.FindAction("Inventory").WasPressedThisFrame())
            {
                if (isGifting) CancelGifting();
                else if (isShopping) CancelShopping();
                return;
            }
        }
        else if (dialoguePanel != null && dialoguePanel.activeInHierarchy)
        {
            if (inventoryObject.activeSelf)
            {
                inventoryObject.SetActive(false);
                inventoryDisplay.tooltipPanel.GetComponent<TooltipUI>()?.HideTooltip();
            }
            return;
        }

        if (InputSystem.actions.FindAction("Inventory").WasPressedThisFrame())
        {
            ToggleInventory();
        }
    }

    private void CancelGifting()
    {
        InventoryManager.Instance.isGifting = false;

        if (inventoryDisplay != null && inventoryDisplay.tooltipPanel != null)
        {
            inventoryDisplay.tooltipPanel.GetComponent<TooltipUI>()?.HideTooltip();
        }

        var contextMenu = FindFirstObjectByType<ItemContextMenu>(FindObjectsInactive.Include);
        if (contextMenu != null) contextMenu.Hide();

        if (inventoryObject != null) inventoryObject.SetActive(false);

        DialogueManager dm = FindFirstObjectByType<DialogueManager>();
        if (dm != null)
        {
            dm.optionsPanel.SetActive(true);
        }
    }

    private void CancelShopping()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.isShopping = false;
        }

        if (inventoryDisplay != null && inventoryDisplay.tooltipPanel != null)
        {
            inventoryDisplay.tooltipPanel.GetComponent<TooltipUI>()?.HideTooltip();
        }

        var contextMenu = FindFirstObjectByType<ItemContextMenu>(FindObjectsInactive.Include);
        if (contextMenu != null) contextMenu.Hide();

        if (inventoryObject != null) inventoryObject.SetActive(false);

        DialogueManager dm = FindFirstObjectByType<DialogueManager>();
        if (dm != null)
        {
            dm.CloseShopUI();
        }
    }

    private void ToggleInventory()
    {
        if (inventoryObject == null) return;

        bool isOpening = !inventoryObject.activeSelf;
        inventoryObject.SetActive(isOpening);

        if (isOpening && inventoryDisplay != null)
        {
            inventoryDisplay.RefreshUI();
        }
    }

    public void UpdateUI(int current, int max)
    {
        hpBar.maxValue = max;
        hpBar.value = current;
        hpText.text = $"{FormatHP(current)}/{FormatHP(max)}";

        if (current < (int)hpBar.value) StartCoroutine(ShakeHPBar());
        StartCoroutine(FlashFill());
    }

    public void ShowHPText() { hpText.enabled = true; Gold.SetActive(true); }
    public void HideHPText() { hpText.enabled = false; Gold.SetActive(false); }

    string FormatHP(int value)
    {
        if (value >= 1_000_000) return (value / 1_000_000f).ToString("0.#") + "M";
        if (value >= 1_000) return (value / 1_000f).ToString("0.#") + "k";
        return value.ToString();
    }

    IEnumerator ShakeHPBar(float duration = 0.2f, float magnitude = 10f)
    {
        Vector3 originalPos = hpBarTransform.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            hpBarTransform.localPosition = originalPos + new Vector3(Random.Range(-1f, 1f) * magnitude, Random.Range(-1f, 1f) * magnitude, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        hpBarTransform.localPosition = originalPos;
    }

    IEnumerator FlashFill(float duration = 0.2f)
    {
        fillImage.color = flashColor;
        yield return new WaitForSeconds(duration);
        fillImage.color = normalColor;
    }
}