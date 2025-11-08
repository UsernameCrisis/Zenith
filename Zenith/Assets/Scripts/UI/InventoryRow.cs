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
}
