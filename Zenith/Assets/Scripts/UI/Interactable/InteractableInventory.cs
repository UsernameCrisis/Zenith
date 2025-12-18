using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InteractableInventory : InteractableObject, Interactable
{
    private Inventory inventory;
    public ItemTable LootTable;
    private List<DraggableItem> items= new List<DraggableItem>();
    private bool _isInitialized = false;
    public DraggableItem DraggableItemPrefab;
    public GameObject Object
    {
        get { return gameObject; }
    }

    public override void OnInteract()
    {
        if (!_isInitialized) InitializeItems();

        FindAnyObjectByType<OverworldUI>().chestUIController.LoadItems(items);

        inventory = FindAnyObjectByType<OverworldUI>().inventory;
        inventory.SetActive(true);
        inventory.GetComponentInChildren<InventoryLeft>().SetInteractableInventoryActive();
        base.OnInteract();
    }

    private void InitializeItems()
    {
        for (int i = 0; i < LootTable.PossibleItems.Count; i++)
        {
            if (Random.Range(0, 100) <= LootTable.ChancePercentage[i])
            {
                DraggableItem draggableItem = Instantiate(DraggableItemPrefab);
                draggableItem.item = LootTable.PossibleItems[i];
                draggableItem.InitializeItemValues();
                draggableItem.SetSprite();
                draggableItem.slotType = DraggableItem.SlotType.Chest;
                draggableItem.quantity = Random.Range(LootTable.MinRange[i], LootTable.MaxRange[i]);
                items.Add(draggableItem);
            }
        }
        _isInitialized = true;
    }
}
