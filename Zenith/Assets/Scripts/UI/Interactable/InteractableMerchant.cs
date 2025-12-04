using UnityEngine;

public class InteractableMerchant : InteractableObject, Interactable
{
    public GameObject Inventory;
    public void OnInteract()
    {
        Inventory.SetActive(true);
        Inventory.GetComponentInChildren<InventoryLeft>().SetNoneActive();
        Inventory.GetComponent<Inventory>().isSelling = true;
    }
}
