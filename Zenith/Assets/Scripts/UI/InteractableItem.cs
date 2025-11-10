using Unity.VisualScripting;
using UnityEngine;

public class InteractableItem : MonoBehaviour, Interactable
{

    public DraggableItem item;
    public GameObject Object
    {
        get { return gameObject; }
    }

    public void OnInteract()
    {
        Debug.Log("Interacted");
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
