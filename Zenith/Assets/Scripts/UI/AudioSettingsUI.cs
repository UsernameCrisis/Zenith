using UnityEngine;
using UnityEngine.UI;
using System;

public class AudioSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private GameObject prevMenu;
    [SerializeField] private Button back;
    public event Action<string> OnButtonSelected;

    void Awake()
    {
        back.onClick.AddListener(() => OnButtonSelected?.Invoke("Back"));
    }

    void Start()
    {
        masterSlider.value = PlayerPrefs.GetFloat("masterVolume", 0.75f);
        musicSlider.value = PlayerPrefs.GetFloat("musicVolume", 0.75f);
        sfxSlider.value = PlayerPrefs.GetFloat("soundFXVolume", 0.75f);
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Back()
    {
        Hide();
        prevMenu.SetActive(true);
    }

    public void OnMasterChange() => AudioManager.Instance.SetMaster(masterSlider.value);
    public void OnMusicChange() => AudioManager.Instance.SetMusic(musicSlider.value);
    public void OnSFXChange() => AudioManager.Instance.SetSFX(sfxSlider.value);
}
