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
    private List<List<ItemData>> _items = new List<List<ItemData>>();
    private List<ItemData> _equipments = new List<ItemData>();
    public bool hasData = false;

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


    public void SaveAndLoadScene(string SceneName)
    {
        PlayerOverworldAttributes player = FindAnyObjectByType<PlayerOverworldAttributes>();

        playerHP = player.currentHP;
        playerMaxHP = player.maxHP;
        gold = player.gold;
        playerAtk = player.Atk;
        playerDef = player.Def;

        DraggableItemToItemData(player.inventory.SaveInventoryIn2DList(), player.inventory.GetEquipmentAsList());
        hasData = true;

        SceneManager.LoadScene(SceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!hasData) return;

        PlayerOverworldAttributes player = FindAnyObjectByType<PlayerOverworldAttributes>();
        player.currentHP = playerHP;
        playerMaxHP = player.maxHP;
        player.gold = gold;
        player.Atk = playerAtk;
        player.Def = playerDef;

        player.inventory.LoadInventory(_items, _equipments);
        player.UpdateStats();
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
    public void ClearInventoryData()
    {
        _items.Clear();
        _equipments.Clear();
    }
}
