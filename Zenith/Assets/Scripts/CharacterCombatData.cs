using TMPro;
using UnityEngine;

public class CharacterCombatData : MonoBehaviour
{
    [SerializeField] private GameObject parent;
    [SerializeField] private TMP_Text HPText;
    private Character character;
    void Start()
    {
        character = parent.GetComponent<Character>();
        UpdateHP();
    }

    void Update()
    {
    }

    private void UpdateHP()
    {
        HPText.text = character.GetHP().ToString();
    }
}
