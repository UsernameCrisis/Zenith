using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

public class InventoryRow : MonoBehaviour
{
    [SerializeField] private InventorySlot[] slots = new InventorySlot[6];

    public bool RowIsEmpty()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].ContainsItem())
            {
                return false;
            }
        }
        return true;
    }

    public bool RowHasEmptySlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].ContainsItem())
            {
                return true;
            }
        }
        return false;
    }

    public void AddItem(DraggableItem item)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].ContainsItem())
            {
                slots[i].SetItem(item);
                return;
            }
        }
    }

    public List<DraggableItem> GetAsList()
    {
        List<DraggableItem> list = new List<DraggableItem>();

        for (int i = 0; i < slots.Length; i++)
        {
            list.Add(slots[i].GetSlotItem());
        }

        return list;
    }

    public void UnloadList(List<DraggableItem> item )
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].UnloadItem(item[i]);
        }
    }

    public bool HasThisItem(DraggableItem item)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].HasThisItem(item))
            {
                return true;
            }
        }
        return false;
    }
    public void AddExistingItem(DraggableItem item)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].HasThisItem(item))
            {
                slots[i].AddExistingItem(item);
            }
        }
    }
}
