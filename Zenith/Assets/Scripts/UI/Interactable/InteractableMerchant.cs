using UnityEngine;

public class InteractableMerchant : InteractableObject, Interactable
{
    public GameObject Inventory;
    public void OnInteract()
    {
        Inventory = FindAnyObjectByType<OverworldUI>().inventory.gameObject;
        Inventory.SetActive(true);
        Inventory.GetComponent<Inventory>().isSelling = true;
        Inventory.GetComponentInChildren<InventoryLeft>().SetShopActive();
    }
}
