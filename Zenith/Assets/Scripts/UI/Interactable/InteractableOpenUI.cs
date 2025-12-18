using UnityEngine;

public class InteractableOpenUI : InteractableObject, Interactable
{
    [Header("UI Setup")]
    [Tooltip("Assign the UI panel (GameObject) you want to show on interact.")]
    public GameObject uiPanel;

    [Header("Optional Settings")]
    [Tooltip("Should the UI toggle on/off each time you interact?")]
    public bool toggleUI = true;

    private bool isOpen = false;

    private void Awake()
    {
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }
    }

    public override void OnInteract()
    {
        if (uiPanel == null) return;

        if (toggleUI)
        {
            isOpen = !isOpen;
            uiPanel.SetActive(isOpen);
        }
        else
        {
            uiPanel.SetActive(true);
        }
    }
}
