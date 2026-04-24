using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Added for InputField references

public class MainMenu : MonoBehaviour
{
    [Header("Menus")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject optionMenu;
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private GameObject ollamaAdminPanel; // New Panel

    [Header("Ollama Admin UI")]
    [SerializeField] private TMP_InputField urlInput;
    [SerializeField] private TMP_InputField convModelInput;
    [SerializeField] private TMP_InputField combatModelInput;
    [SerializeField] private OllamaChatProvider ollamaProvider;

    private bool isInsideOption = false;
    private AudioSettingsUI setting;

    void Awake()
    {
        setting = optionMenu.GetComponent<AudioSettingsUI>();
        setting.OnButtonSelected += HandleOptionMenu;

        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (ollamaAdminPanel != null) ollamaAdminPanel.SetActive(false);

        InitializeOllamaUI();
    }

    private void InitializeOllamaUI()
    {
        if (ollamaProvider != null)
        {
            ollamaProvider.LoadSettings();

            urlInput.text = ollamaProvider.activeUrl;
            convModelInput.text = ollamaProvider.conversationModel;
            combatModelInput.text = ollamaProvider.combatModel;
        }
    }

    public void ApplyOllamaSettings()
    {
        if (ollamaProvider != null)
        {
            ollamaProvider.SaveSettings(urlInput.text, convModelInput.text, combatModelInput.text);
            Debug.Log("<color=green>Ollama Settings Updated & Saved!</color>");
        }
        CloseOllamaAdmin();
    }

    public void OpenOllamaAdmin()
    {
        ollamaAdminPanel.SetActive(true);
        mainMenu.SetActive(false);
    }

    public void CloseOllamaAdmin()
    {
        ollamaAdminPanel.SetActive(false);
        mainMenu.SetActive(true);
    }

    public void Options()
    {
        mainMenu.SetActive(false);
        optionMenu.SetActive(true);
        isInsideOption = true;
    }

    public void HandleOptionMenu(string button)
    {
        if (button == "Back")
        {
            setting.Back();
            isInsideOption = false;
        }
    }

    public void NewGame() => SceneManager.LoadScene("Tavern");
    public void LoadSave() => SceneManager.LoadScene("Tavern");

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
        if (InventoryManager.Instance != null) InventoryManager.Instance.DeleteSaveFile();
        if (GameManager.Instance != null) GameManager.Instance.DeleteGameState();

        Debug.Log("<color=red>All save files deleted.</color>");
        CloseDeleteConfirmation();
    }

    public void QuitGame() => Application.Quit();
}