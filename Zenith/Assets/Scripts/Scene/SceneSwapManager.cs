using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwapManager : MonoBehaviour
{
    public static SceneSwapManager instance;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public static void SWAP_SCENE(SceneField myScene)
    {
        instance.StartCoroutine(instance.ChangeScene(myScene));
    }

    private IEnumerator ChangeScene(SceneField myScene)
    {
        SceneManager.LoadScene(myScene);
        yield return null;
    }
}
