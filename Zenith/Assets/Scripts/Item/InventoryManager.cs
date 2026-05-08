using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.IO;

[System.Serializable]
public class ItemStack
{
    public BaseItem itemData;
    public int quantity;

    public ItemStack(BaseItem item, int amount)
    {
        itemData = item;
        quantity = amount;
    }
}

[System.Serializable]
public class InventorySaveData
{
    public float savedMaxWeight;
    public List<string> mainInventoryIDs = new();
    public List<int> mainInventoryAmounts = new();

    public List<EquipmentItem.EquipSlot> equippedSlots = new();
    public List<string> equippedIDs = new();
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public event Action OnInventoryChanged;
    public bool isGifting;

    [Header("Inventory Settings")]
    public List<ItemStack> mainInventory = new();
    public float maxWeight = 80.0f;
    public float currentWeight;

    public Dictionary<EquipmentItem.EquipSlot, EquipmentItem> equippedItems = new();

    [Header("Save System")]
    public ItemDatabase database;
    private string savePath;

    void Awake()
    {
        savePath = Application.persistentDataPath + "/save.json";

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (File.Exists(savePath))
        {
            LoadInventory();
            Debug.Log("Auto-loaded previous save.");
        }
        else
        {
            Debug.Log("No save file found. Starting fresh.");
        }
    }

    #region Save/Load Logic

    public void SaveInventory()
    {
        InventorySaveData data = new InventorySaveData();
        data.savedMaxWeight = maxWeight;

        foreach (var stack in mainInventory)
        {
            if (stack.itemData != null)
            {
                data.mainInventoryIDs.Add(stack.itemData.itemID);
                data.mainInventoryAmounts.Add(stack.quantity);
            }
        }

        foreach (var kvp in equippedItems)
        {
            if (kvp.Value != null)
            {
                data.equippedSlots.Add(kvp.Key);
                data.equippedIDs.Add(kvp.Value.itemID);
            }
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log("Game Saved to: " + savePath);
    }

    public void LoadInventory()
    {
        if (!File.Exists(savePath)) return;

        string json = File.ReadAllText(savePath);
        InventorySaveData data = JsonUtility.FromJson<InventorySaveData>(json);

        maxWeight = data.savedMaxWeight;

        mainInventory.Clear();
        for (int i = 0; i < data.mainInventoryIDs.Count; i++)
        {
            BaseItem item = database.GetItemByID(data.mainInventoryIDs[i]);
            if (item != null)
            {
                mainInventory.Add(new ItemStack(item, data.mainInventoryAmounts[i]));
            }
        }

        equippedItems.Clear();
        for (int i = 0; i < data.equippedIDs.Count; i++)
        {
            BaseItem item = database.GetItemByID(data.equippedIDs[i]);
            if (item is EquipmentItem equip)
            {
                equippedItems[data.equippedSlots[i]] = equip;
            }
        }

        UpdateWeight();
        RefreshTotalStats();
        OnInventoryChanged?.Invoke();
    }

    public void DeleteSaveFile()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("Save file deleted!");

            mainInventory.Clear();
            equippedItems.Clear();
            maxWeight = 80.0f;
            UpdateWeight();
            RefreshTotalStats();
            OnInventoryChanged?.Invoke();
        }
    }

    #endregion

    public void SortInventory()
    {
        mainInventory = mainInventory
            .OrderBy(stack => GetTypePriority(stack.itemData.type))
            .ThenByDescending(stack => (int)stack.itemData.rarity)
            .ThenBy(stack => stack.itemData.itemName)
            .ToList();

        PrintInventoryDebug();
    }

    private int GetTypePriority(ItemType type)
    {
        return type switch
        {
            ItemType.Equipment => 0,
            ItemType.Consumable => 1,
            ItemType.Junk => 2,
            _ => 3
        };
    }

    public bool AddItem(BaseItem item, int amount)
    {
        if (currentWeight + (item.weight * amount) > maxWeight)
        {
            Debug.Log("<color=red>Too heavy to carry!</color>");
            return false;
        }

        bool foundStack = false;
        if (item.isStackable)
        {
            ItemStack existingStack = mainInventory.Find(slot => slot.itemData == item);
            if (existingStack != null)
            {
                existingStack.quantity += amount;
                foundStack = true;
            }
        }

        if (!foundStack)
        {
            mainInventory.Add(new ItemStack(item, amount));
        }

        UpdateWeight();
        SortInventory();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void RemoveItem(BaseItem item, int amount = 1)
    {
        ItemStack stack = mainInventory.Find(s => s.itemData == item);
        if (stack != null)
        {
            stack.quantity -= amount;
            if (stack.quantity <= 0)
            {
                mainInventory.Remove(stack);
            }
        }
        UpdateWeight();
        OnInventoryChanged?.Invoke();
    }

    public void GiftItem(ItemStack stack)
    {
        if (stack.itemData == null) return;

        DialogueManager dm = FindFirstObjectByType<DialogueManager>();
        if (dm != null)
        {
            dm.ReceiveGift(stack.itemData);
        }

        RemoveItem(stack.itemData, 1);

        isGifting = false;

        OverworldUI ui = FindFirstObjectByType<OverworldUI>();
        if (ui != null && ui.inventoryObject != null)
        {
            ui.inventoryObject.SetActive(false);

            if (ui.inventoryDisplay != null && ui.inventoryDisplay.tooltipPanel != null)
            {
                ui.inventoryDisplay.tooltipPanel.SetActive(false);
            }
        }
    }

    public void UseItem(ItemStack stack)
    {
        if (stack.itemData == null) return;

        stack.itemData.Use();

        if (stack.itemData.type == ItemType.Consumable)
        {
            RemoveItem(stack.itemData, 1);
        }

        OnInventoryChanged?.Invoke();
    }

    public void UpdateWeight()
    {
        float newWeight = 0;

        foreach (ItemStack stack in mainInventory)
        {
            if (stack.itemData != null)
                newWeight += stack.itemData.weight * stack.quantity;
        }

        foreach (var item in equippedItems.Values)
        {
            if (item != null)
                newWeight += item.weight;
        }

        currentWeight = newWeight;
    }

    public void HandleDeathPenalty()
    {
        int itemsToLose = mainInventory.Count / 2;
        for (int i = 0; i < itemsToLose; i++)
        {
            if (mainInventory.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, mainInventory.Count);
                mainInventory.RemoveAt(randomIndex);
            }
        }

        if (GameManager.Instance != null)
        {
            int goldToLose = Mathf.FloorToInt(GameManager.Instance.gold * 0.10f);

            if (goldToLose > 100)
            {
                goldToLose = 100;
            }

            GameManager.Instance.gold -= goldToLose;
            Debug.Log($"<color=yellow>Gold Penalty:</color> Lost {goldToLose} gold.");

            var player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerOverworldAttributes>();
            if (player != null)
            {
                player.gold = GameManager.Instance.gold;
            }
        }

        UpdateWeight();
        OnInventoryChanged?.Invoke();

        SaveInventory();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGameState();
        }

        Debug.Log("<color=red>Death Penalty Applied and Saved to Disk.</color>");
    }

    public void EquipItem(EquipmentItem newItem)
    {
        if (equippedItems.ContainsKey(newItem.slot))
        {
            ForceUnequip(newItem.slot);
        }

        RemoveItem(newItem, 1);
        equippedItems[newItem.slot] = newItem;

        RefreshTotalStats();
        UpdateWeight();
        SortInventory();
        OnInventoryChanged?.Invoke();
    }

    private void ForceUnequip(EquipmentItem.EquipSlot slot)
    {
        if (equippedItems.TryGetValue(slot, out EquipmentItem item))
        {
            equippedItems.Remove(slot);

            ItemStack existingStack = mainInventory.Find(s => s.itemData == item);
            if (item.isStackable && existingStack != null)
                existingStack.quantity++;
            else
                mainInventory.Add(new ItemStack(item, 1));
        }
    }

    public void UnequipItem(EquipmentItem.EquipSlot slot)
    {
        ForceUnequip(slot);
        RefreshTotalStats();
        UpdateWeight();
        OnInventoryChanged?.Invoke();
    }

    public void RefreshTotalStats()
    {
        int totalAtk = 10;
        int totalDef = 0;
        int totalSpeed = 5;
        int totalMaxHP = 100;

        foreach (var item in equippedItems.Values)
        {
            totalAtk += item.attackBonus;
            totalDef += item.defenseBonus;
            totalSpeed += item.speedBonus;
            totalMaxHP += item.maxHpBonus;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerAtk = totalAtk;
            GameManager.Instance.playerDef = totalDef;
            GameManager.Instance.playerSpeed = totalSpeed;
            GameManager.Instance.playerMaxHP = totalMaxHP;
        }

        var player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerOverworldAttributes>();
        if (player != null)
        {
            player.maxHP = totalMaxHP;
            player.currentHP = Mathf.Min(player.currentHP, player.maxHP);
        }
    }

    private void PrintInventoryDebug()
    {
        string debugLog = "--- CURRENT INVENTORY ---\n";
        debugLog += $"Total Weight: {currentWeight}/{maxWeight}\n";
        debugLog += "---------------------------\n";

        foreach (var stack in mainInventory)
        {
            debugLog += $"[{stack.itemData.type}] {stack.itemData.itemName} x{stack.quantity}\n";
        }

        Debug.Log(debugLog);
    }
}