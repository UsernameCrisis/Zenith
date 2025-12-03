using Unity.VisualScripting;
using UnityEngine;

public class InteractableInventory : InteractableObject, Interactable
{
    public Inventory inventory;
    public GameObject ChestUI;
    public GameObject Object
    {
        get { return gameObject; }
    }

    public override void OnInteract()
    {
        inventory.SetActive(true);
        inventory.GetComponentInChildren<InventoryLeft>().SetInteractableInventoryActive();
        base.OnInteract();
    }
}
