using System;
using System.Data.Common;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTrigger : MonoBehaviour
{
    [HideInInspector] public GameObject InteractUI;
    public GameObject CurrentInteractable;
    public GameObject CurrentOpenInteractable;
    [HideInInspector] public TMP_Text _interactText;

    void OnTriggerEnter(Collider other)
    {
        if (InteractUI == null && _interactText == null) FindMissingComponents();
        if (other.CompareTag("Interactable") && CurrentInteractable == null)
        {
            InteractUI.SetActive(true);
            CurrentInteractable = other.gameObject;
            _interactText.text = CurrentInteractable.GetComponent<InteractableObject>().InteractText;

            CurrentOpenInteractable = null;
        }       
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Interactable"))
        {
            TurnOff();
            if (FindAnyObjectByType<OverworldUI>().inventory.isActiveAndEnabled) FindAnyObjectByType<OverworldUI>().inventory.SetActive(false);
        }  
    }

    public void TurnOff() 
    {
        InteractUI.SetActive(false);
        CurrentInteractable = null;
        // CurrentOpenInteractable = null;
    }

    void Update()
    {
        if (CurrentInteractable == null) return;
        if (InputSystem.actions.FindAction("Interact").WasPressedThisFrame())
        {
            CurrentOpenInteractable = CurrentInteractable;
            CurrentInteractable.GetComponent<Interactable>().OnInteract();
        }
    }

    private void FindMissingComponents()
    {
        try
        {
            InteractUI = FindAnyObjectByType<OverworldUI>().interactUI;
            _interactText = InteractUI.transform.GetChild(1).GetComponent<TMP_Text>();
        }
        catch (Exception ex)
        {
        }
    }
}
