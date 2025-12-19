using UnityEngine;

public abstract class InteractableObject : MonoBehaviour, Interactable
{
    public string InteractText;

    public virtual void OnInteract()
    {
        FindAnyObjectByType<PlayerTrigger>().TurnOff();
    }

    public virtual void ItemTaken(ItemData itemData)
    {
    }
}
