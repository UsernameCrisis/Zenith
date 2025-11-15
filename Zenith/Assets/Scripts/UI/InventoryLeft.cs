using UnityEngine;

public class InventoryLeft : MonoBehaviour
{
    public enum Panels
    {
        Chest,
        Gear
    }

    public Panels CurrentPanel = Panels.Gear;
    public GameObject GearPanel;
    // public GameObject ChestItemsUI;

    public void SetGearActive()
    {
        // ChestItemsUI.SetActive(false);
        GearPanel.SetActive(true);
    }
    
    public void SetInteractableInventoryActive()
    {
        GearPanel.SetActive(false);
        // ChestItemsUI.SetActive(true);
    }
}
