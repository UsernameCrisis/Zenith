using System;
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
        if (InteractUI == null || _interactText == null) FindMissingComponents();

        if (other.CompareTag("Interactable") && CurrentInteractable == null)
        {
            InteractUI.SetActive(true);
            CurrentInteractable = other.gameObject;

            var interactable = CurrentInteractable.GetComponent<InteractableObject>();
            if (interactable != null)
            {
                _interactText.text = interactable.InteractText;
            }

            CurrentOpenInteractable = null;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Interactable"))
        {
            TurnOff();

            var overworldUI = FindAnyObjectByType<OverworldUI>();
            if (overworldUI != null && overworldUI.inventoryObject != null)
            {
                if (overworldUI.inventoryObject.activeInHierarchy)
                {
                    overworldUI.inventoryObject.SetActive(false);
                }
            }
        }
    }

    public void TurnOff()
    {
        if (InteractUI != null) InteractUI.SetActive(false);
        CurrentInteractable = null;
    }

    void Update()
    {
        if (CurrentInteractable == null) return;

        if (InputSystem.actions.FindAction("Interact").WasPressedThisFrame())
        {
            CurrentOpenInteractable = CurrentInteractable;

            var interactable = CurrentInteractable.GetComponent<Interactable>();
            if (interactable != null)
            {
                interactable.OnInteract();
            }
        }
    }

    private void FindMissingComponents()
    {
        try
        {
            var overworldUI = FindAnyObjectByType<OverworldUI>();
            if (overworldUI != null)
            {
                InteractUI = overworldUI.interactUI;
                _interactText = InteractUI.transform.GetChild(1).GetComponent<TMP_Text>();
            }
        }
        catch (Exception)
        {
            // Silently fail if UI isn't found yet
        }
    }
}