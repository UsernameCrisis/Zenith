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
}
