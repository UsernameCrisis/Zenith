using UnityEngine;

public abstract class InteractableObject : MonoBehaviour, Interactable
{
    public GameObject Object => throw new System.NotImplementedException();

    public void OnInteract()
    {
        throw new System.NotImplementedException();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) FindAnyObjectByType<PlayerHealthUI>().ToggleInteractUI(this.gameObject);
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) FindAnyObjectByType<PlayerHealthUI>().ToggleInteractUI(this.gameObject);
    }
}
