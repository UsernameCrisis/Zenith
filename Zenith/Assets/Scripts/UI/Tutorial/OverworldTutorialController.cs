using UnityEngine;

public class OverworldTutorialController : MonoBehaviour
{
    public TutorialController TutorialObject;

    void Start()
    {
        if (!GameManager.Instance.ViewedOverworldTutorial)
        {
            TutorialObject.Open();
            GameManager.Instance.ViewedOverworldTutorial = true;
        }
    }
}
