using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuTavern : MonoBehaviour
{
    private bool isPaused = false;
    private bool isInsideOption = false, isInsideTutorial = false;

    private InputAction escapeKeyAction;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private AudioSettingsUI optionMenu;
    [SerializeField] private TutorialMenu tutorialMenu;

    void OnEnable()
    {
        pauseMenu.OnButtonSelected += HandlePauseMenu;
        optionMenu.OnButtonSelected += HandleOptionMenu;
        tutorialMenu.OnButtonSelected += HandleTutorialMenu;
    }
    void OnDisable()
    {
        pauseMenu.OnButtonSelected -= HandlePauseMenu;
        optionMenu.OnButtonSelected -= HandleOptionMenu;
        tutorialMenu.OnButtonSelected -= HandleTutorialMenu;
    }

    void Awake()
    {
        escapeKeyAction = InputSystem.actions.FindAction("ExitSelect");
    }

    void Update()
    {
        if (escapeKeyAction.WasPressedThisFrame()) HandleEscapePressed();
    }

    private void HandleEscapePressed()
    {

        if (isInsideTutorial)
        {
            tutorialMenu.Back();
            isInsideTutorial = false;
            return;
        }

        if (isInsideOption)
        {
            optionMenu.Back();
            isInsideOption = false;
            return;
        }

        if (isPaused)
        {
            Resume();
            return;
        }

        OpenPauseMenu();
    }

    private void Resume()
    {
        Time.timeScale = 1;
        isPaused = false;
        pauseMenu.Hide();
    }

    private void OpenPauseMenu()
    {
        pauseMenu.Show();
        isPaused = true;
        Time.timeScale = 0;
    }

    private void HandleOptionMenu(string button)
    {
        if (button == "Back")
        {
            optionMenu.Back();
            isInsideOption = false;
        }
    }
    private void HandleTutorialMenu(string button)
    {
        if (button == "Back")
        {
            tutorialMenu.Back();
            isInsideTutorial = false;
        }
    }

    private void HandlePauseMenu(string button)
    {
        if (button == "Resume")
        {
            Resume();
        }
        else if (button == "Settings")
        {
            pauseMenu.Hide();
            optionMenu.Show();
            isInsideOption = true;

        }
        else if (button == "Tutorial")
        {
            pauseMenu.Hide();
            tutorialMenu.Show();
            isInsideTutorial = true;
        }
        else if (button == "Exit")
        {
            Time.timeScale = 1;
            SceneManager.LoadScene("Main Menu");
        }
    }
}
