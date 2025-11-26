using Unity.VisualScripting;
using UnityEngine;

public class InteractableItem : InteractableObject, Interactable
{

    public Item item;
    public GameObject Object;
    public Inventory inventory;

    public override void OnInteract()
    {
        // transform.parent.GetComponentInParent<SceneRoot>().MainUI.inventory.AddItem(item);
        // transform.parent.GetComponentInParent<SceneRoot>().MainUI.interactUI.SetActive(false);
        // transform.parent.GetComponentInParent<SceneRoot>().MainUI.EmptyCurrentInteractable();
        inventory.AddItem(item);
        Destroy(this.gameObject);
        base.OnInteract();
    }
}
