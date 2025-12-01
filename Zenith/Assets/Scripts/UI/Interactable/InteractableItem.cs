using Unity.VisualScripting;
using UnityEngine;

public class InteractableItem : InteractableObject, Interactable
{

    public Item item;
    public DraggableItem DraggableItemPrefab;
    public GameObject Object;
    public Inventory inventory;

    private void Start() {
        inventory = FindAnyObjectByType<OverworldUI>().inventory;
    }

    public override void OnInteract()
    {
        // transform.parent.GetComponentInParent<SceneRoot>().MainUI.inventory.AddItem(item);
        // transform.parent.GetComponentInParent<SceneRoot>().MainUI.interactUI.SetActive(false);
        // transform.parent.GetComponentInParent<SceneRoot>().MainUI.EmptyCurrentInteractable();
        DraggableItem newDraggableItem = Instantiate(DraggableItemPrefab);
        newDraggableItem.item = item;
        inventory.AddItem(newDraggableItem);
        Destroy(this.gameObject);
        base.OnInteract();
    }
}
