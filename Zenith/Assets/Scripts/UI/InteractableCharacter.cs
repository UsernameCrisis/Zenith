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
        // transform.parent.GetComponentInParent<SceneRoot>().dialogBox.gameObject.SetActive(true);
        // transform.parent.GetComponentInParent<SceneRoot>().dialogBox.PlayDialog(dialog);
    }
}
