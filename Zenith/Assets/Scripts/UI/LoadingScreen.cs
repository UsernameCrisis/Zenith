using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private Image loadingCircle;
    [SerializeField] private Canvas canvas;

    public void show(Camera _mainCamera)
    {
        SceneManager.LoadScene("Loading_Scene", LoadSceneMode.Additive);
        canvas.worldCamera = _mainCamera;
    }

    public void disable()
    {
        SceneManager.UnloadSceneAsync("Loading_Scene");
    }

    void FixedUpdate()
    {
        loadingCircle.fillAmount += 0.05f;
        if (loadingCircle.fillAmount == 1) loadingCircle.fillAmount = 0;
    }
}
