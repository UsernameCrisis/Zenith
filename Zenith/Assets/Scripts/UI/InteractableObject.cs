using UnityEngine;

public abstract class InteractableObject : MonoBehaviour, Interactable
{
    public GameObject Object => throw new System.NotImplementedException();

    public void OnInteract()
    {
        throw new System.NotImplementedException();
    }
}
