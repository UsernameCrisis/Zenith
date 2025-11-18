using Unity.VisualScripting;
using UnityEngine;

public class InteractableInventory : MonoBehaviour, Interactable
{
    public Inventory inventory;
    public GameObject ChestUI;
    public GameObject Object
    {
        get { return gameObject; }
    }

    public void OnInteract()
    {
        inventory.SetActive(true);
        inventory.GetComponentInChildren<InventoryLeft>().SetInteractableInventoryActive();
    }
}
