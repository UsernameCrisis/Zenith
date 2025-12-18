using System.Collections.Generic;
using UnityEngine;

public class ChestUIController : MonoBehaviour
{
    [SerializeField] private InventorySlot[] slot = new InventorySlot[24];

    public void LoadItems(List<DraggableItem> items)
    {
        ClearUI();

        LoadUI(items);
    }

    private void LoadUI(List<DraggableItem> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null) continue;
            slot[i].SetItem(items[i]);
        }
    }

    private void ClearUI()
    {
        for (int i = 0; i < slot.Length; i++)
        {
            if (!slot[i].ContainsItem()) return;
            slot[i].RemoveItem();
        }
    }
}
