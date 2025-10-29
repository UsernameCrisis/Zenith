using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MainCanvasManager : MonoBehaviour
{
    public static MainCanvasManager Instance { get; private set; }
    

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadPrefabs();
    }

    public void LoadPrefabs()
    {
    }

    //Get Prefab(GameObject) by name
    public void Activate(string name)
    {
    }

    public void Deactivate(string name)
    {
    }
}
