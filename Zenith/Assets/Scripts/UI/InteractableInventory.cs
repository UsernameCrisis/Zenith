using Unity.VisualScripting;
using UnityEngine;

public class InteractableInventory : MonoBehaviour, Interactable
{
    public GameObject Object
    {
        get { return gameObject; }
    }

    public void OnInteract()
    {
        // transform.parent.GetComponentInParent<SceneRoot>().MainUI.inventory.SetActive(true);
        // transform.parent.GetComponentInParent<SceneRoot>().ChestUI.gameObject.SetActive(true);
        // transform.parent.GetComponentInParent<SceneRoot>().MainUI.inventory.GetComponentInChildren<InventoryLeft>().SetInteractableInventoryActive();
    }
}
