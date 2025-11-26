using System.Data.Common;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTrigger : MonoBehaviour
{
    public GameObject InteractUI;
    private GameObject CurrentInteractable;
    public TMP_Text _interactText;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Interactable") && CurrentInteractable == null)
        {
            InteractUI.SetActive(true);
            CurrentInteractable = other.gameObject;
            _interactText.text = CurrentInteractable.GetComponent<InteractableObject>().InteractText;
        }       
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Interactable"))
        {
            InteractUI.SetActive(false);
            CurrentInteractable = null;
        }  
    }

    public void TurnOff() 
    {
        InteractUI.SetActive(false);
        CurrentInteractable = null;
    }

    void Update()
    {
        if (CurrentInteractable == null) return;
        if (InputSystem.actions.FindAction("Interact").WasPressedThisFrame())
        {
            CurrentInteractable.GetComponent<Interactable>().OnInteract();
        }
    }
}
