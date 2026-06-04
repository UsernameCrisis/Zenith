using UnityEngine.UI;
using UnityEngine;
using System;
using TMPro;

public class CombatOverMenu : MonoBehaviour
{
    public Button ok;
    public event Action<string> OnButtonSelected;
    [SerializeField] private TextMeshProUGUI resultText;
    private bool playerWon;

    void Awake()
    {
        ok.onClick.AddListener(HandleOkPressed);
        gameObject.SetActive(false);
    }

    private void HandleOkPressed()
    {
        OnButtonSelected?.Invoke(playerWon ? "Victory" : "Defeat");
    }

    public void Show(bool won)
    {
        playerWon = won;
        gameObject.SetActive(true);
        resultText.text = won ? "Combat Won!" : "Combat Lost!";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
