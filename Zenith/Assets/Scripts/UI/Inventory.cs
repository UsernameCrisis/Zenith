using UnityEngine;

public class Inventory : MonoBehaviour
{
    public bool expandable = false;
    private bool active = true;
    public void SetActive(bool b)
    {
        gameObject.SetActive(b);
    }
    public void ToggleInventory()
    {
        active = !active;
        gameObject.SetActive(active);
    }
    public void AddItem(Item item)
    {
        GetComponentInChildren<InventoryRight>().AddItem(item);
    }
}
