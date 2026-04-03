using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public EquipmentItem.EquipSlot slotType;
    public Image itemIcon;
    public Sprite emptySlotSprite;

    private EquipmentItem currentItem;
    private TooltipUI tooltip;

    void Start()
    {
        tooltip = FindAnyObjectByType<TooltipUI>(FindObjectsInactive.Include);
        InventoryManager.Instance.OnInventoryChanged += RefreshSlot;
        RefreshSlot();
    }

    public void RefreshSlot()
    {
        if (InventoryManager.Instance.equippedItems.TryGetValue(slotType, out EquipmentItem item))
        {
            currentItem = item;
            itemIcon.sprite = item.icon;
            itemIcon.color = Color.white;
        }
        else
        {
            currentItem = null;
            itemIcon.sprite = emptySlotSprite;
            itemIcon.color = new Color(1, 1, 1, 0.5f);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && currentItem != null)
        {
            tooltip?.HideTooltip();
            InventoryManager.Instance.UnequipItem(slotType);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem != null && tooltip != null)
            tooltip.UpdateTooltip(currentItem);
    }

    public void OnPointerExit(PointerEventData eventData) => tooltip?.HideTooltip();
}