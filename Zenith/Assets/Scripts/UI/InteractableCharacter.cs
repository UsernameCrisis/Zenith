using System;
using System.Collections.Generic;
using UnityEngine;

public class InteractableCharacter : InteractableObject, Interactable
{
    List<String> dialog = new List<string>{"dialog1", "dialog2", "dialog3"};
    
    public GameObject Object
    {
        get { return gameObject; }
    }

    public void OnInteract()
    {
        Debug.Log("1");
        transform.parent.GetComponentInParent<SceneRoot>().dialogBox.gameObject.SetActive(true);
        transform.parent.GetComponentInParent<SceneRoot>().dialogBox.PlayDialog(dialog);
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
