using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTrigger : MonoBehaviour
{
    [Header("Blocking UI")]
    [Tooltip("UI panels that should block interaction when active.")]
    public List<GameObject> blockingUIPanels = new List<GameObject>();

    [HideInInspector] public GameObject InteractUI;
    public GameObject CurrentInteractable;
    public GameObject CurrentOpenInteractable;
    [HideInInspector] public TMP_Text _interactText;

    private bool IsAnyUIActive()
    {
        foreach (GameObject panel in blockingUIPanels)
        {
            if (panel != null && panel.activeInHierarchy)
            {
                return true;
            }
        }
        return false;
    }
    void OnTriggerEnter(Collider other)
    {
        if (InteractUI == null || _interactText == null) FindMissingComponents();

        if (other.CompareTag("Interactable") && CurrentInteractable == null)
        {
            CurrentInteractable = other.gameObject;

            if (!IsAnyUIActive())
            {
                ShowInteractPrompt();
            }

            CurrentOpenInteractable = null;
        }
    }

    private void ShowInteractPrompt()
    {
        if (InteractUI == null) return;

        InteractUI.SetActive(true);
        var interactable = CurrentInteractable.GetComponent<InteractableObject>();
        if (interactable != null && _interactText != null)
        {
            _interactText.text = interactable.InteractText;
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

        if (IsAnyUIActive())
        {
            if (InteractUI.activeInHierarchy) InteractUI.SetActive(false);
        }
        else
        {
            if (!InteractUI.activeInHierarchy && CurrentInteractable != null)
            {
                ShowInteractPrompt();
            }
        }

        if (!IsAnyUIActive() && InputSystem.actions.FindAction("Interact").WasPressedThisFrame())
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