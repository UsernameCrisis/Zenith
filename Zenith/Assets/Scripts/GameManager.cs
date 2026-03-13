using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int playerHP = 90;
    public int playerMaxHP = 100;
    public int gold = 0;
    //
    public int playerAtk;
    public int playerDef;
    private List<List<ItemData>> _items = new();
    private List<ItemData> _equipments = new();
    public bool hasData = false;
    public bool ViewedOverworldTutorial = false;

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
            activeTrigger.CleanupDefeatedEnemies(defeatedEnemyNames);
        }
    }
    public void SaveAndLoadScene(string SceneName)
    {
        PlayerOverworldAttributes player = FindAnyObjectByType<PlayerOverworldAttributes>();
        
        playerHP = player.currentHP;
        playerMaxHP = player.maxHP;
        gold = player.gold;

        DraggableItemToItemData(player.inventory.SaveInventoryIn2DList(), player.inventory.GetEquipmentAsList());
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

        player.inventory.LoadInventory(_items, _equipments);
        UpdateStats();
    }

    private void DraggableItemToItemData(List<List<DraggableItem>> items, List<DraggableItem> equipments)
    {
        _items.Clear();
        _equipments.Clear();

        for (int i = 0; i < items.Count; i++)
        {
            _items.Add(new List<ItemData>());
            for (int j = 0; j < items[i].Count; j++)
            {
                try {_items[i].Add(items[i][j].GetData());} catch (Exception e) {_items[i].Add(null);}
            }
        }

        for (int i = 0; i < equipments.Count; i++)
        {
            try {_equipments.Add(equipments[i].GetData());} catch (Exception e) {_equipments.Add(null);}
        }
    }

    public void UpdateStats()
    {
        playerDef = FindAnyObjectByType<OverworldUI>().inventory.GetArmor();
        playerAtk = FindAnyObjectByType<OverworldUI>().inventory.GetATK();
    }
    public void ClearInventoryData()
    {
        _items.Clear();
        _equipments.Clear();
    }

}
