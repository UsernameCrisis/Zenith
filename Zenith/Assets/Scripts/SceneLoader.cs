using UnityEngine;

public class SceneLoader : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        LoadingScreenManager.Load(sceneName);
    }
}