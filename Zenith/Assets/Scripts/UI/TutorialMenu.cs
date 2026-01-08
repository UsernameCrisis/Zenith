using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

public class TutorialMenu : MonoBehaviour
{
    [SerializeField] private GameObject LeftButton;
    [SerializeField] private GameObject RightButton;
    [SerializeField] private GameObject CloseButton;
    [SerializeField] private Image image;
    [SerializeField] private TMP_Text description;
    [SerializeField] private List<TutorialItem> items;
    [SerializeField] private GameObject prevMenu;
    [SerializeField] private Button back;
    public event Action<string> OnButtonSelected;
    private int index = 0;
    void Awake()
    {
        back.onClick.AddListener(() => OnButtonSelected?.Invoke("Back"));
    }
    void Start()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        if (items.Count == 0) return;
        index = 0;

        gameObject.SetActive(true);
        LeftButton.SetActive(false);
        CloseButton.SetActive(false);
        
        if (items.Count == 1)
        {
            RightButton.SetActive(false);
            CloseButton.SetActive(true);
        }
        else RightButton.SetActive(true);

        image.sprite = items[index].image;
        description.text = items[index].description;
    }

    public void Hide()
    {
        LeftButton.SetActive(true); 
        RightButton.SetActive(true);
        CloseButton.SetActive(true);
        gameObject.SetActive(false);
    }

    public void Next()
    {
        if (!LeftButton.activeSelf) LeftButton.SetActive(true);

        index++;
        image.sprite = items[index].image;
        description.text = items[index].description;

        if (index + 1 == items.Count) {RightButton.SetActive(false); CloseButton.SetActive(true);}
    }
    public void Previous()
    {
        if (!RightButton.activeSelf) RightButton.SetActive(true);

        index--;
        image.sprite = items[index].image;
        description.text = items[index].description;
        
        if (index - 1 == -1) LeftButton.SetActive(false);
    }

    public void Back()
    {
        Hide();
        prevMenu.SetActive(true);
    }

}
