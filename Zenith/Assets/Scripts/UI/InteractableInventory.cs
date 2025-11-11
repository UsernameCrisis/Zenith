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
        transform.parent.GetComponentInParent<SceneRoot>().MainUI.inventory.SetActive(true);
        transform.parent.GetComponentInParent<SceneRoot>().ChestUI.gameObject.SetActive(true);
        transform.parent.GetComponentInParent<SceneRoot>().MainUI.inventory.GetComponentInChildren<InventoryLeft>().SetInteractableInventoryActive();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !transform.parent.GetComponentInParent<SceneRoot>().MainUI.InteractUIIsActive())
        {
            transform.parent.GetComponentInParent<SceneRoot>().MainUI.ToggleInteractUI(this.gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && transform.parent.GetComponentInParent<SceneRoot>().MainUI.InteractUIIsActive())
        {
            transform.parent.GetComponentInParent<SceneRoot>().MainUI.ToggleInteractUI(this.gameObject);
        }
        if (other.CompareTag("Player") && transform.parent.GetComponentInParent<SceneRoot>().MainUI.inventory.gameObject.active)
        {
            transform.parent.GetComponentInParent<SceneRoot>().MainUI.inventory.ToggleInventory();
        }
        
        if (other.CompareTag("Player") && transform.parent.GetComponentInParent<SceneRoot>().ChestUI.gameObject.active)
        {
            transform.parent.GetComponentInParent<SceneRoot>().ChestUI.gameObject.SetActive(false);
        }
    }
}
