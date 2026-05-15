using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemContextMenu : MonoBehaviour
{
    [Header("UI References")]
    public Button actionButton;
    public TMP_Text actionText;
    public Button trashButton;
    public TMP_Text trashText;

    private ItemStack currentStack;
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition))
            {
                Hide();
            }
        }
    }

    public void Show(ItemStack stack, Vector2 position)
    {
        currentStack = stack;
        gameObject.SetActive(true);
        transform.position = position;

        actionButton.gameObject.SetActive(false);
        switch (stack.itemData.type)
        {
            case ItemType.Equipment:
                actionButton.gameObject.SetActive(true);
                actionText.text = "Equip";
                break;
            case ItemType.Consumable:
                actionButton.gameObject.SetActive(true);
                actionText.text = "Use";
                break;
        }

        if (InventoryManager.Instance.isGifting)
        {
            trashText.text = "Gift";
        }
        else if (InventoryManager.Instance.isShopping)
        {
            trashText.text = "Sell";
        }
        else
        {
            trashText.text = "Trash";
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    public void OnActionClick()
    {
        if (currentStack != null)
        {
            InventoryManager.Instance.UseItem(currentStack);
            if (!InventoryManager.Instance.mainInventory.Contains(currentStack))
                Hide();
        }
    }

    public void OnTrashClick()
    {
        if (currentStack == null) return;

        if (InventoryManager.Instance.isGifting)
        {
            InventoryManager.Instance.GiftItem(currentStack);
            Hide();
        }
        else if (InventoryManager.Instance.isShopping)
        {
            InventoryManager.Instance.SellItem(currentStack);

            if (!InventoryManager.Instance.mainInventory.Contains(currentStack))
                Hide();
        }
        else
        {
            InventoryManager.Instance.RemoveItem(currentStack.itemData, 1);
            if (!InventoryManager.Instance.mainInventory.Contains(currentStack))
                Hide();
        }
    }

    public void Hide() => gameObject.SetActive(false);
}