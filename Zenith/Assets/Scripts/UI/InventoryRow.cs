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

    public void AddItemToNextEmptySlot(Item item)
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

    public List<Item> GetAsList()
    {
        List<Item> list = new List<Item>();

        for (int i = 0; i < slots.Length; i++)
        {
            list.Add(slots[i].GetSlotItem());
        }

        return list;
    }

    public void UnloadList(List<Item> item )
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].UnloadItem(item[i]);
        }
    }
}
