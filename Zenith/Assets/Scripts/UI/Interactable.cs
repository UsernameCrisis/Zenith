using UnityEngine;

public interface Interactable
{
    GameObject Object { get; }
    void OnInteract();
}
