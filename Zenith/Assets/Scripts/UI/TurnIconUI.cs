using UnityEngine;
using UnityEngine.UI;

public class TurnIconUI : MonoBehaviour
{
    [SerializeField] private Image portrait;
    [SerializeField] private Image frame;

    public void Set(CharacterObject character)
    {
        portrait.sprite = character.Portrait;
        frame.color = character.IsPlayer ? Color.cyan : Color.red;
    }
}
