using UnityEngine;

[CreateAssetMenu(fileName = "TutorialItem", menuName = "Scriptable Objects/TutorialItem")]
public class TutorialItem : ScriptableObject
{
    public Sprite image;
    public string description;
}
