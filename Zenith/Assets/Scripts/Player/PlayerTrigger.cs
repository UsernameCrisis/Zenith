using UnityEngine;

public class PlayerTrigger : MonoBehaviour
{
    public GameObject InteractUI;
    private int CurrentInteractableID = 0;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Interactable") && CurrentInteractableID == 0)
        {
            InteractUI.SetActive(true);
            CurrentInteractableID = other.gameObject.GetInstanceID();
        }       
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Interactable"))
        {
            InteractUI.SetActive(false);
            CurrentInteractableID = 0;
        }  
    }
}
