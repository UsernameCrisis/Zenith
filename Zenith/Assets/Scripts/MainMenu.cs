using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void NewGame()
    {
        // Load your first scene (make sure it�s added to Build Settings)
        SceneManager.LoadScene("SampleScene");

        GameManager.Instance.Player.gameObject.SetActive(true);
    }

    public void LoadSave()
    {
        Debug.Log("Load Save clicked!");
    }

    public void Options()
    {
        Debug.Log("Options clicked!");
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game clicked!");
        Application.Quit();
    }
}
