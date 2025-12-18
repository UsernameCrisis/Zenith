using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ConfirmationUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text confirmationText;
    public Button confirmButton;
    public Button closeButton;

    private string targetScene;

    private void Awake()
    {
        gameObject.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseUI);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmTravel);
    }

    public void OpenConfirmation(string sceneName)
    {
        targetScene = sceneName;
        if (confirmationText != null)
            confirmationText.text = $"Travel to {sceneName}?";

        gameObject.SetActive(true);
    }

    private void CloseUI()
    {
        gameObject.SetActive(false);
        targetScene = null;
    }

    private void ConfirmTravel()
    {
        if (!string.IsNullOrEmpty(targetScene))
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(targetScene);
        }
    }
}
