using System.Collections;
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] private string mainMenuScene = "Main Menu";

    private void Awake()
    {   
        if (LoadingScreenManager.Instance == null)
        {
            GameObject managerPrefab = Resources.Load<GameObject>("LoadingScreenManager");
            if (managerPrefab != null)
                Instantiate(managerPrefab);
            else
                Debug.LogError("Bootstrap: Could not find LoadingScreenManager prefab in Resources!");
        }
    }

    private IEnumerator Start()
    {
        yield return null;

        LoadingScreenManager.Load(mainMenuScene);
    }
}
