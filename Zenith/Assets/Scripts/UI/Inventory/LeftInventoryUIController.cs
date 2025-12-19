using System.Collections.Generic;
using UnityEngine;

public class LeftInventoryUIController : MonoBehaviour
{
    [SerializeField] private InventorySlot[] slot = new InventorySlot[24];
    public DraggableItem DraggableItemPrefab;

    public void LoadItems(List<ItemData> items)
    {
        ClearUI();

        LoadUI(items);
    }

    private void LoadUI(List<ItemData> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null) continue;
            DraggableItem newDraggableItem = Instantiate(DraggableItemPrefab);
            newDraggableItem.SetData(items[i]);
            newDraggableItem.SetSprite();
            slot[i].SetItem(newDraggableItem);
        }
    }

    private void ClearUI()
    {
        for (int i = 0; i < slot.Length; i++)
        {
            if (slot[i].ContainsItem()) slot[i].RemoveItem();
            slot[i].ClearQuantity();
        }
    }
}
