using UnityEngine;

public class InteractableNPC : InteractableObject, Interactable
{
    [Header("Dialogue Content")]
    [Tooltip("The DialogueData ScriptableObject for this NPC.")]
    public DialogueData conversation;

    [Header("Manager Reference")]
    [Tooltip("If left empty, it will try to find the DialogueManager in the scene.")]
    public DialogueManager dialogueManager;

    private void Awake()
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindFirstObjectByType<DialogueManager>();
        }
    }

    public override void OnInteract()
    {
        if (conversation == null)
        {
            Debug.LogWarning($"No DialogueData assigned to {gameObject.name}!");
            return;
        }

        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(conversation);
        }
        else
        {
            Debug.LogError("No DialogueManager found in the scene!");
        }
    }
}