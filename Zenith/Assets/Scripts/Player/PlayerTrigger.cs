using System.Data.Common;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTrigger : MonoBehaviour
{
    public GameObject InteractUI;
    private GameObject CurrentInteractable;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Interactable") && CurrentInteractable == null)
        {
            InteractUI.SetActive(true);
            CurrentInteractable = other.gameObject;
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
