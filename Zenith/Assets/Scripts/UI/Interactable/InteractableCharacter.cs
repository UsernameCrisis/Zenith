using System;
using System.Collections.Generic;
using UnityEngine;

public class InteractableCharacter : InteractableObject, Interactable
{
    public Dialog dialog;
    public DialogBox DialogBox;

    public void OnInteract()
    {
        DialogBox.PlayDialog(dialog);
    }
}
