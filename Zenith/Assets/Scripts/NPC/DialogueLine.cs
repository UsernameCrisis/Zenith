using UnityEngine;

[System.Serializable]
public struct DialogueLine
{
    public string name;
    public Sprite characterPortrait;
    public bool isPlayer;
    [TextArea(3, 10)]
    public string text;
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Conversation")]
public class DialogueData : ScriptableObject
{
    public DialogueLine[] lines;
}