using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventoryRowUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image itemIcon;
    public TMP_Text itemName;
    public TMP_Text itemAmount;

    private BaseItem itemData;
    private InventoryDisplayUI parentUI;

    public void Setup(ItemStack stack, InventoryDisplayUI ui)
    {
        itemData = stack.itemData;
        parentUI = ui;

        itemIcon.sprite = itemData.icon;
        itemName.text = itemData.itemName;

        itemAmount.text = stack.quantity > 1 ? $"x{stack.quantity}" : "";
    }

    public void OnPointerEnter(PointerEventData eventData) => parentUI.ShowTooltip(itemData);
    public void OnPointerExit(PointerEventData eventData) => parentUI.HideTooltip();
}