using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuOverworldInput : MonoBehaviour
{
    private bool isPaused = false;
    private bool isInsideOption = false;

    private InputAction escapeKeyAction;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private AudioSettingsUI optionMenu;
    [SerializeField] private GameObject confirmationModal;

    void OnEnable()
    {
        pauseMenu.OnButtonSelected += HandlePauseMenu;
        optionMenu.OnButtonSelected += HandleOptionMenu;
    }
    void OnDisable()
    {
        pauseMenu.OnButtonSelected -= HandlePauseMenu;
        optionMenu.OnButtonSelected -= HandleOptionMenu;
    }

    void Awake()
    {
        escapeKeyAction = InputSystem.actions.FindAction("ExitSelect");
        if (confirmationModal != null)
        {
            confirmationModal.SetActive(false);
        }
    }

    void Update()
    {
        if (escapeKeyAction.WasPressedThisFrame()) HandleEscapePressed();
    }

    private void HandleEscapePressed()
    {
        if (confirmationModal != null && confirmationModal.activeSelf)
        {
            confirmationModal.SetActive(false);
            return;
        }

        if (isPaused && !isInsideOption)
        {
            Resume();
        }
        else if (isInsideOption)
        {
            optionMenu.Back();
            isInsideOption = false;
        }
        else
        {
            OpenPauseMenu();
        }
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
        else if (button == "Exit")
        {
            print("Exit pressed");
        }
    }
}
