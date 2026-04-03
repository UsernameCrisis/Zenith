using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Image itemIcon;
    public TMP_Text itemName;
    public TMP_Text itemAmount;

    private ItemStack currentStack;
    private InventoryDisplayUI parentUI;

    public void Setup(ItemStack stack, InventoryDisplayUI ui)
    {
        currentStack = stack;
        parentUI = ui;

        itemIcon.sprite = currentStack.itemData.icon;
        itemName.text = currentStack.itemData.itemName;
        itemAmount.text = currentStack.quantity > 1 ? $"x{currentStack.quantity}" : "";

        if (itemName != null)
            itemName.color = GetRarityColor(currentStack.itemData.rarity);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (parentUI != null && parentUI.tooltipPanel != null)
        {
            var tooltip = parentUI.tooltipPanel.GetComponent<TooltipUI>();
            if (tooltip != null)
            {
                tooltip.UpdateTooltip(currentStack.itemData);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (parentUI != null && parentUI.tooltipPanel != null)
        {
            parentUI.tooltipPanel.GetComponent<TooltipUI>()?.HideTooltip();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Force hide tooltip when context menu opens
            parentUI.tooltipPanel.GetComponent<TooltipUI>()?.HideTooltip();

            var contextMenu = FindAnyObjectByType<ItemContextMenu>(FindObjectsInactive.Include);
            if (contextMenu != null)
            {
                contextMenu.Show(currentStack, eventData.position);
            }
        }
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => new Color(0.8f, 0.8f, 0.8f),
            ItemRarity.Uncommon => Color.green,
            ItemRarity.Rare => Color.cyan,
            ItemRarity.Epic => Color.magenta,
            ItemRarity.Legendary => new Color(1f, 0.5f, 0f),
            _ => Color.white
        };
    }
}