using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class PurchasableItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Item Configuration")]
    public BaseItem shopItem;

    [Header("UI References")]
    public Image itemIcon;
    public TMP_Text priceText;

    [Header("Visual Feedback Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color errorColor = Color.red;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 8f;

    private TooltipUI tooltip;
    private bool isAnimating = false;
    private Vector3 originalIconPosition;

    void Start()
    {
        tooltip = FindAnyObjectByType<TooltipUI>(FindObjectsInactive.Include);
        originalIconPosition = itemIcon.transform.localPosition;
        InitializeSlot();
    }

    public void InitializeSlot()
    {
        if (shopItem == null) return;

        itemIcon.sprite = shopItem.icon;
        itemIcon.color = normalColor;

        if (priceText != null)
        {
            priceText.text = (shopItem.sellPrice * 4).ToString();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left || eventData.button == PointerEventData.InputButton.Right)
        {
            TryPurchaseItem();
        }
    }

    private void TryPurchaseItem()
    {
        if (shopItem == null || GameManager.Instance == null || InventoryManager.Instance == null) return;

        int calculatedBuyPrice = shopItem.sellPrice * 4;

        bool hasEnoughGold = GameManager.Instance.gold >= calculatedBuyPrice;

        bool canCarry = (InventoryManager.Instance.currentWeight + shopItem.weight) <= InventoryManager.Instance.maxWeight;

        if (hasEnoughGold && canCarry)
        {
            ExecutePurchase(calculatedBuyPrice);
        }
        else
        {
            TriggerFailureFeedback();
        }
    }

    private void ExecutePurchase(int finalCost)
    {
        GameManager.Instance.gold -= finalCost;

        var playerAttr = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerOverworldAttributes>();
        if (playerAttr != null)
        {
            playerAttr.gold = GameManager.Instance.gold;
        }

        InventoryManager.Instance.AddItem(shopItem, 1);

        if (tooltip != null && tooltip.gameObject.activeSelf)
        {
            tooltip.UpdateTooltip(shopItem);
        }

        Debug.Log($"Successfully purchased: {shopItem.itemName} for {finalCost}g");
    }

    private void TriggerFailureFeedback()
    {
        if (!isAnimating)
        {
            StartCoroutine(FailureFeedbackRoutine());
        }
    }

    private IEnumerator FailureFeedbackRoutine()
    {
        isAnimating = true;
        itemIcon.color = errorColor;

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float randomX = Random.Range(-1f, 1f) * shakeMagnitude;
            float randomY = Random.Range(-1f, 1f) * shakeMagnitude;

            itemIcon.transform.localPosition = originalIconPosition + new Vector3(randomX, randomY, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        itemIcon.transform.localPosition = originalIconPosition;
        itemIcon.color = normalColor;
        isAnimating = false;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (shopItem != null && tooltip != null)
        {
            tooltip.UpdateTooltip(shopItem);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltip?.HideTooltip();
    }
}