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
            // Check if the mouse is NOT over the context menu rect
            if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition))
            {
                // We clicked outside, so hide.
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
            case ItemType.Junk:
                break;
        }

        DialogueManager dm = FindFirstObjectByType<DialogueManager>();
        if (dm == null || !dm.dialoguePanel.activeInHierarchy)
        {
            InventoryManager.Instance.isGifting = false;
        }
        trashText.text = InventoryManager.Instance.isGifting ? "Gift" : "Trash";

        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    public void OnActionClick()
    {
        Debug.Log("Action Button Clicked!");
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
        else
        {
            Debug.Log("Trash Button Clicked!");
            InventoryManager.Instance.RemoveItem(currentStack.itemData, 1);

            if (!InventoryManager.Instance.mainInventory.Contains(currentStack))
            {
                Hide();
            }
        }
    }

    public void Hide() => gameObject.SetActive(false);


}