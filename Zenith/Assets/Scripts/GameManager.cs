using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

[System.Serializable]
public class NPCSaveData
{
    public string npcID;
    public int friendship;
    public int dailyTalks;
}
[System.Serializable]
public class GameSaveData
{
    public int gold;
    public int gold_spent;
    public bool viewedTutorial;
    public List<NPCSaveData> npcDataList = new List<NPCSaveData>();
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Persistent Stats")]
    public int gold = 0;
    public int gold_spent = 0;
    public Dictionary<string, NPCSaveData> npcRegistry = new Dictionary<string, NPCSaveData>();

    [Header("Combat Stats")]
    public int playerHP = 100;
    public int playerMaxHP = 100;
    public int playerAtk = 10;
    public int playerDef = 0;
    public int playerSpeed = 5;

    [Header("Tutorial")]
    public bool hasData = false;
    public bool ViewedOverworldTutorial = false;

    [Header("Combat Transition Settings")]
    public string combatSceneName = "Combat_test1";
    public List<string> currentEncounterEnemyNames = new();
    public List<string> defeatedEnemyNames = new();

    private GameObject currentOverworldRoot;
    private EnemyTrigger activeTrigger;
    private string savePath;

    private void Awake()
    {
        savePath = Application.persistentDataPath + "/gamestate.json";

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            LoadGameState();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public NPCSaveData GetNPCData(string id)
    {
        if (!npcRegistry.ContainsKey(id))
        {
            npcRegistry[id] = new NPCSaveData { npcID = id, friendship = 0, dailyTalks = 0 };
        }
        return npcRegistry[id];
    }

    public void UpdateNPC(string id, int friendshipChange, bool incrementTalk)
    {
        NPCSaveData data = GetNPCData(id);

        data.friendship = Mathf.Clamp(data.friendship + friendshipChange, -100, 2000);

        if (incrementTalk) data.dailyTalks++;

        Debug.Log($"{id} updated. Friendship: {data.friendship}, Talks: {data.dailyTalks}");
    }

    public void ResetDailyTalkLimits()
    {
        foreach (var npc in npcRegistry.Values)
        {
            npc.dailyTalks = 0;
        }
        Debug.Log("NPC Talk Limits have been reset for this visit.");
    }

    #region Save/Load Logic
    public void SaveGameState()
    {
        GameSaveData data = new GameSaveData();
        data.gold = gold;
        data.gold_spent = gold_spent;
        data.viewedTutorial = ViewedOverworldTutorial;

        data.npcDataList = new List<NPCSaveData>(npcRegistry.Values);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
    }

    public void LoadGameState()
    {
        if (!File.Exists(savePath)) return;

        string json = File.ReadAllText(savePath);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

        gold = data.gold;
        gold_spent = data.gold_spent;
        ViewedOverworldTutorial = data.viewedTutorial;

        npcRegistry.Clear();
        foreach (var npc in data.npcDataList)
        {
            npcRegistry[npc.npcID] = npc;
        }

        hasData = true;
    }

    public void DeleteGameState()
    {
        if (File.Exists(savePath)) File.Delete(savePath);

        gold = 0;
        gold_spent = 0;
        ViewedOverworldTutorial = false;
        npcRegistry.Clear();
        hasData = false;
    }

    #endregion


    public void SetOverworldRoot(GameObject root) => currentOverworldRoot = root;

    public void PrepareCombat(List<string> enemyNames, EnemyTrigger trigger)
    {
        currentEncounterEnemyNames = new List<string>(enemyNames);
        defeatedEnemyNames.Clear();
        activeTrigger = trigger;
    }

    public void StartCombatScene()
    {
        if (currentOverworldRoot != null) currentOverworldRoot.SetActive(false);
        SceneManager.LoadScene(combatSceneName, LoadSceneMode.Additive);
    }

    public void EndCombat()
    {
        SceneManager.UnloadSceneAsync(combatSceneName);
        if (currentOverworldRoot != null) currentOverworldRoot.SetActive(true);
        if (activeTrigger != null) activeTrigger.CleanupDefeatedEnemies(defeatedEnemyNames);
    }

    public void SaveAndLoadScene(string SceneName)
    {
        PlayerOverworldAttributes player = FindAnyObjectByType<PlayerOverworldAttributes>();
        if (player != null)
        {
            playerHP = player.currentHP;
            playerMaxHP = player.maxHP;
            gold = player.gold;
            hasData = true;

            SaveGameState();
        }

        SceneManager.LoadScene(SceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!hasData) return;

        string name = scene.name;
        if (name == "Combat_test1" || name == "Main Menu") return;

        PlayerOverworldAttributes player = FindAnyObjectByType<PlayerOverworldAttributes>();
        if (player != null)
        {
            player.currentHP = playerHP;
            player.maxHP = playerMaxHP;
            player.gold = gold;
        }
    }
}