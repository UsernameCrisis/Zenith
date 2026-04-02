using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Presist Inventory")]
    public int gold = 0;
    public int gold_spent = 0;

    [Header("Combat Stats")]
    public int playerHP = 100;
    private readonly int basePlayerMaxHp = 100;
    public int playerMaxHP = 100;
    private readonly int basePlayerAtk = 10;
    public int playerAtk = 10;
    private readonly int basePlayerDef = 0;
    public int playerDef = 0;
    private readonly int basePlayerSpeed = 5;
    public int playerSpeed = 5;

    [Header("Tutorial")]
    public bool hasData = false; //hook to playerprefs later
    public bool ViewedOverworldTutorial = false; //hook to playerprefs later

    [Header("Combat Transition Settings")]
    public string combatSceneName = "Combat_test1";
    public List<string> currentEncounterEnemyNames = new();
    public List<string> defeatedEnemyNames = new();

    private GameObject currentOverworldRoot;
    private EnemyTrigger activeTrigger;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetOverworldRoot(GameObject root)
    {
        currentOverworldRoot = root;
    }

    public void PrepareCombat(List<string> enemyNames, EnemyTrigger trigger)
    {
        currentEncounterEnemyNames = new List<string>(enemyNames);
        defeatedEnemyNames.Clear();
        activeTrigger = trigger;
    }

    public void StartCombatScene()
    {
        // BUGTEST: Log the enemies we are carrying into combat
        if (currentEncounterEnemyNames.Count > 0)
        {
            string enemyList = string.Join(", ", currentEncounterEnemyNames);
            Debug.Log($"<color=orange>Combat Starting!</color> Enemies detected: {enemyList}");
        }
        else
        {
            Debug.LogWarning("Combat started, but no enemy names were captured!");
        }

        // 1. Disable the Overworld Root to "pause" the world
        if (currentOverworldRoot != null)
        {
            currentOverworldRoot.SetActive(false);
            Debug.Log("Overworld Root disabled.");
        }
        else
        {
            Debug.LogError("No Overworld Root found! Make sure your OverworldAnchor is set up.");
        }

        // 2. Load the combat scene without destroying the Overworld
        // Use LoadSceneMode.Additive so both technically exist in the hierarchy
        SceneManager.LoadScene(combatSceneName, LoadSceneMode.Additive);
    }

    public void EndCombat()
    {
        SceneManager.UnloadSceneAsync(combatSceneName);

        if (currentOverworldRoot != null)
            currentOverworldRoot.SetActive(true);

        if (activeTrigger != null)
        {
            //IMPORTANT sementara kan aku manual pasang enemynya jadi namanya agak aneh, nanti kedepan aku ubah ke otomatis sesuai dengan nama enemy dari listnya
            activeTrigger.CleanupDefeatedEnemies(defeatedEnemyNames);
        }
    }
    public void SaveAndLoadScene(string SceneName)
    {
        PlayerOverworldAttributes player = FindAnyObjectByType<PlayerOverworldAttributes>();
        
        playerHP = player.currentHP;
        playerMaxHP = player.maxHP;
        gold = player.gold;
        hasData = true;

        SceneManager.LoadScene(SceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!hasData) return;

        if(SceneManager.GetActiveScene().name == "Combat_test1" || SceneManager.GetActiveScene().name == "Main Menu")
        {
            return;
        }

        PlayerOverworldAttributes player = FindAnyObjectByType<PlayerOverworldAttributes>();
        player.currentHP = playerHP;
        playerMaxHP = player.maxHP;
        player.gold = gold;
    }
}
