using UnityEngine;

public class InteractableNPC : InteractableObject, Interactable
{
    [Header("NPC Identity")]
    public string npcID;

    [Header("Dialogue Content")]
    public DialogueData conversation;

    [Header("Manager Reference")]
    public DialogueManager dialogueManager;

    private void Awake()
    {
        if (dialogueManager == null)
            dialogueManager = FindFirstObjectByType<DialogueManager>();
    }

    public override void OnInteract()
    {
        if (conversation == null) return;

        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(conversation, npcID);
        }
    }
}