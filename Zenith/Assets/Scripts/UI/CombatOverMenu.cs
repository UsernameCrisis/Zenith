using UnityEngine.UI;
using UnityEngine;
using System;
using TMPro;

public class CombatOverMenu : MonoBehaviour
{
    public Button ok;
    public event Action<string> OnButtonSelected;
    public TextMeshProUGUI resultText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        ok.onClick.AddListener(() => OnButtonSelected?.Invoke("ok"));
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Show(bool won)
    {
        gameObject.SetActive(true);
        resultText.text = won ? "Combat Won!" : "Combat Lost!";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
