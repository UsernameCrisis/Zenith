using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject optionMenu;
    [SerializeField] private GameObject confirmationPanel;

    private bool isInsideOption = false;
    private AudioSettingsUI setting;

    public void NewGame()
    {
        SceneManager.LoadScene("Tavern");
    }

    public void LoadSave()
    {
        SceneManager.LoadScene("Tavern");
    }

    // --- Delete Save Logic ---

    public void OpenDeleteConfirmation()
    {
        confirmationPanel.SetActive(true);
        mainMenu.SetActive(false);
    }

    public void CloseDeleteConfirmation()
    {
        confirmationPanel.SetActive(false);
        mainMenu.SetActive(true);
    }

    public void ConfirmDeleteSave()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.DeleteSaveFile();

        if (GameManager.Instance != null)
            GameManager.Instance.DeleteGameState();

        Debug.Log("<color=red>All save files deleted from Main Menu.</color>");

        CloseDeleteConfirmation();
    }

    // --- Existing Logic ---

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

        if (confirmationPanel != null) confirmationPanel.SetActive(false);
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