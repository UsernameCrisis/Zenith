using UnityEngine;
using UnityEngine.SceneManagement;

public class MainSceneManager : MonoBehaviour
{
    // public static MainSceneManager Instance { get; private set; }

    // void Awake()
    // {
    //     if (Instance != null && Instance != this)
    //     {
    //         Destroy(gameObject);
    //         return;
    //     }

    //     Instance = this;
    //     DontDestroyOnLoad(gameObject);
    // }

    // public static void LoadScene(string SceneName)
    // {
    //     SceneManager.LoadScene(SceneName);
    //     GameManager.Instance.Player = FindAnyObjectByType<Player>();
    // }
}
