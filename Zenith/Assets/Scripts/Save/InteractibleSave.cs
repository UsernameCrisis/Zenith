using UnityEngine;
using System.Collections;

public class SaveInteractable : InteractableObject, Interactable
{
    [Header("UI Feedback")]
    [Tooltip("The 'Game Saved!' UI panel or text object.")]
    public GameObject saveFeedbackUI;

    [Tooltip("How long the message stays on screen.")]
    public float displayDuration = 2.0f;

    private Coroutine hideRoutine;

    private void Awake()
    {
        if (saveFeedbackUI != null)
        {
            saveFeedbackUI.SetActive(false);
        }
    }

    public override void OnInteract()
    {
        SaveGame();

        if (saveFeedbackUI != null)
        {
            if (hideRoutine != null) StopCoroutine(hideRoutine);

            saveFeedbackUI.SetActive(true);
            hideRoutine = StartCoroutine(HideSaveMessage());
        }
    }

    private void SaveGame()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SaveInventory();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGameState();
        }

        Debug.Log("<color=green>Game Saved via Interactable!</color>");
    }

    private IEnumerator HideSaveMessage()
    {
        yield return new WaitForSeconds(displayDuration);
        saveFeedbackUI.SetActive(false);
        hideRoutine = null;
    }
}