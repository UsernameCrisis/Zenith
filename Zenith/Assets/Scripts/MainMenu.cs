using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject optionMenu;
    private bool isInsideOption = false;
    private AudioSettingsUI setting;
    public void NewGame()
    {
        SceneManager.LoadScene("Peaceful");
    }

    public void LoadSave()
    {
        Debug.Log("Load Save clicked!");
    }

    public void Options()
    {
        mainMenu.SetActive(false);
        optionMenu.SetActive(true);
        isInsideOption = true;
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game clicked!");
        Application.Quit();
    }

    void Awake()
    {
        setting = optionMenu.GetComponent<AudioSettingsUI>();
        setting.OnButtonSelected += HandleOptionMenu;
    }

    public void HandleOptionMenu(string button)
    {
        if (button == "Back")
        {
            setting.Back();
            isInsideOption = false;
        }
    }
}
