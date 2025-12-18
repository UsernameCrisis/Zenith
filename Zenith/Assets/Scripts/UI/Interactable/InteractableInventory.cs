using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InteractableInventory : InteractableObject, Interactable
{
    private Inventory inventory;
    public ChestItems LootTable;
    private List<DraggableItem> itemDatas= new List<DraggableItem>();
    public GameObject Object
    {
        get { return gameObject; }
    }

    public override void OnInteract()
    {
        inventory = FindAnyObjectByType<OverworldUI>().inventory;
        inventory.SetActive(true);
        inventory.GetComponentInChildren<InventoryLeft>().SetInteractableInventoryActive();
        base.OnInteract();
    }

    private void InitializeItems()
    {
        for (int i = 0; i < LootTable.PossibleItems.Count; i++)
        {
            if (Random.Range(0, 100) > LootTable.ChancePercentage[i])
            {
                DraggableItem draggableItem = new DraggableItem();
                draggableItem.item = LootTable.PossibleItems[i];
                draggableItem.InitializeItemValues();
                draggableItem.quantity = Random.Range(LootTable.MinRange[i], LootTable.MaxRange[i]);
            }
        }
    }
}
