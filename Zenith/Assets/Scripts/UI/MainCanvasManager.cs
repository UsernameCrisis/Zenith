using Unity.VisualScripting;
using UnityEngine;

public class MainCanvasManager : MonoBehaviour
{
    public static MainCanvasManager Instance { get; private set;}
    [SerializeField] private CanvasObject[] CanvasObjects;

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
        for (int i = 0; i < CanvasObjects.Length; i++)
        {
            CanvasObjects[i].prefab.transform.parent = GameManager.Instance.MainCanvas.transform;
        }

        //enable main menu after load
        Activate("MainMenu");
    }

    //Get Prefab(GameObject) by name
    public void Activate(string name)
    {
        for (int i = 0; i < CanvasObjects.Length; i++)
        {
            if (CanvasObjects[i].name == name)
            {
                CanvasObjects[i].prefab.SetActive(true);
                return;
            }
        }
        return;
    }

    public void Deactivate(string name)
    {
        for (int i = 0; i < CanvasObjects.Length; i++)
        {
            if (CanvasObjects[i].name == name)
            {
                CanvasObjects[i].prefab.SetActive(false);
                return;
            }
        }
        return;
    }
}
