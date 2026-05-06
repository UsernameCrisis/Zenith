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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        ok.onClick.AddListener(HandleOkPressed);
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void HandleOkPressed()
    {
        // Tell subscribers what happened so they can react appropriately
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
