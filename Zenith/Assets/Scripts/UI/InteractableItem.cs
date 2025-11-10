using Unity.VisualScripting;
using UnityEngine;

public class InteractableItem : MonoBehaviour, Interactable
{

    public Item item;
    public GameObject Object
    {
        get { return gameObject; }
    }

    public void OnInteract()
    {
        transform.parent.GetComponentInParent<SceneRoot>().MainUI.inventory.AddItem(item);
        transform.parent.GetComponentInParent<SceneRoot>().MainUI.interactUI.SetActive(false);
        transform.parent.GetComponentInParent<SceneRoot>().MainUI.EmptyCurrentInteractable();
        Destroy(this.gameObject);
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
    }
}
