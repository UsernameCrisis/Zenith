using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerCombatMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private AudioSettingsUI optionMenu;
    [SerializeField] private TutorialMenu tutorialMenu;

    private enum MenuState { None, Paused, Options, Tutorial }
    private MenuState state = MenuState.None;

    public bool IsCharacterSelected { get; set; } = false;
    public System.Action OnRequestDeselectCharacter;

    void OnEnable()
    {
        pauseMenu.OnButtonSelected  += HandlePauseMenu;
        optionMenu.OnButtonSelected += HandleOptionMenu;
        tutorialMenu.OnButtonSelected += HandleTutorialMenu;
    }

    void OnDisable()
    {
        pauseMenu.OnButtonSelected  -= HandlePauseMenu;
        optionMenu.OnButtonSelected -= HandleOptionMenu;
        tutorialMenu.OnButtonSelected -= HandleTutorialMenu;
    }

    public void HandleEscapePressed()
    {
        switch (state)
        {
            case MenuState.Tutorial:
                tutorialMenu.Back();
                state = MenuState.None;
                return;

            case MenuState.Options:
                optionMenu.Back();
                state = MenuState.None;
                return;

            case MenuState.Paused:
                Resume();
                return;

            case MenuState.None:
                // If a character is selected, deselect before opening the menu.
                if (IsCharacterSelected)
                {
                    OnRequestDeselectCharacter?.Invoke();
                    return;
                }
                OpenPauseMenu();
                return;
        }
    }

    public bool IsPaused() => state == MenuState.Paused;

    // Menu button handler

    private void HandlePauseMenu(string button)
    {
        switch (button)
        {
            case "Resume":
                Resume();
                break;

            case "Settings":
                pauseMenu.Hide();
                optionMenu.Show();
                state = MenuState.Options;
                break;

            case "Tutorial":
                pauseMenu.Hide();
                tutorialMenu.Show();
                state = MenuState.Tutorial;
                break;

            case "Exit":
                Time.timeScale = 1;
                SceneManager.LoadScene("Main Menu");
                break;
        }
    }

    private void HandleOptionMenu(string button)
    {
        if (button == "Back")
        {
            optionMenu.Back();
            state = MenuState.None;
        }
    }

    private void HandleTutorialMenu(string button)
    {
        if (button == "Back")
        {
            tutorialMenu.Back();
            state = MenuState.None;
        }
    }

    private void Resume()
    {
        Time.timeScale = 1;
        state = MenuState.None;
        pauseMenu.Hide();
    }

    private void OpenPauseMenu()
    {
        pauseMenu.Show();
        state = MenuState.Paused;
        Time.timeScale = 0;
    }
}
