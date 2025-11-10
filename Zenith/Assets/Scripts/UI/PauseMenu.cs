using UnityEngine;
using UnityEngine.UI;
using System;

public class PauseMenu : MonoBehaviour
{
    public Button resume;
    public Button settings;
    public Button exit;
    public event Action<string> OnButtonSelected;

    void Awake()
    {
        resume.onClick.AddListener(() => OnButtonSelected?.Invoke("Resume"));
        settings.onClick.AddListener(() => OnButtonSelected?.Invoke("Settings"));
        exit.onClick.AddListener(() => OnButtonSelected?.Invoke("Exit"));
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        print("show pause");
    }

    public void Hide()
    {
        print("pause hide");
        gameObject.SetActive(false);
    }
}
