using UnityEngine;

public class Inventory : MonoBehaviour
{
    private bool active = true;
    public void ToggleInventory()
    {
        active = !active;
        gameObject.SetActive(active);
        // MainCanvasManager.Instance.PlayerGoldUI.UpdateAmount();
    }
    public void AddItem(Item item)
    {
        GetComponentInChildren<InventoryRight>().AddItem(item);
    }
}
