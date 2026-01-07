using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    public GameObject LeftButton;
    public GameObject RightButton;
    public GameObject CloseButton;
    public UnityEngine.UI.Image image;
    public TMP_Text description;
    public List<TutorialItem> items;
    private int index = 0;
    void Start()
    {
        Close();
    }
    public void Open()
    {
        if (items.Count == 0) return;

        gameObject.SetActive(true);
        LeftButton.SetActive(false);
        CloseButton.SetActive(false);

        if (items.Count == 1) { RightButton.SetActive(false);}
        else {RightButton.SetActive(true);}

        image.sprite = items[index].image;
        description.text = items[index].description;
    }
    public void Close()
    {
        LeftButton.SetActive(true); 
        RightButton.SetActive(true);
        CloseButton.SetActive(false);

        gameObject.SetActive(false);
    }
    public void Next()
    {
        if (!LeftButton.active) LeftButton.SetActive(true);

        index++;
        image.sprite = items[index].image;
        description.text = items[index].description;

        if (index + 1 == items.Count) {RightButton.SetActive(false); CloseButton.SetActive(true);}
    }
    public void Previous()
    {
        if (!RightButton.active) RightButton.SetActive(true);

        index--;
        image.sprite = items[index].image;
        description.text = items[index].description;
        
        if (index - 1 == -1) LeftButton.SetActive(false);
    }
}
