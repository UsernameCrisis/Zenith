using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return null;
            }

            if (instance == null)
            {
                Instantiate(Resources.Load<GameManager>("GameManager"));
            }
#endif
            return instance;
        }
    }

    public Player Player { get; set; }
    [SerializeField] Player PlayerPrefab;
    public Canvas MainCanvas { get; set; }


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }
    private void Initialize()
    {
        MainCanvas = FindAnyObjectByType<Canvas>();

        //Player
        if (Player == null)
        {
            Player = Instantiate(PlayerPrefab);
            Player.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (InputSystem.actions.FindAction("QuickSave").IsPressed())
        {
            SaveSystem.Save();
        }   

        if (InputSystem.actions.FindAction("QuickLoad").IsPressed())
        {
            SaveSystem.Load();
        }   
    }
}
