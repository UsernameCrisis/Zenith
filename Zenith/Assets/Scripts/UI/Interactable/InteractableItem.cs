using Unity.VisualScripting;
using UnityEngine;

public class InteractableItem : InteractableObject, Interactable
{

    public Item item;
    public DraggableItem DraggableItemPrefab;
    public GameObject Object;
    private Inventory inventory;

    private void Start() {
        inventory = FindAnyObjectByType<OverworldUI>().inventory;
    }

    public override void OnInteract()
    {
        DraggableItem newDraggableItem = Instantiate(DraggableItemPrefab);
        newDraggableItem.item = item;
        newDraggableItem.InitializeItemValues();
        if (!inventory.CanInsertToInventory(newDraggableItem)) {Destroy(newDraggableItem); return;}
        inventory.AddItem(newDraggableItem);
        // Destroy(this.gameObject);
        // base.OnInteract();
    }
}
